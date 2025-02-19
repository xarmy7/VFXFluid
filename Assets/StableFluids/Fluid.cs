// StableFluids - A GPU implementation of Jos Stam's Stable Fluids on Unity
// https://github.com/keijiro/StableFluids

using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace StableFluids
{
    public class Fluid : MonoBehaviour
    {
        #region Editable attributes

        [SerializeField] int _resolution = 512;
        [SerializeField] float _viscosity = 1e-6f;
        [SerializeField] float _force = 300;
        [SerializeField] float _exponent = 200;
        [SerializeField] Texture2D _initial;


        [SerializeField] GameObject _player;
        // plane with the material
        [SerializeField] GameObject _target;

        #endregion

        #region Internal resources

        [SerializeField, HideInInspector] ComputeShader _compute;
        [SerializeField, HideInInspector] Shader _shader;

        [SerializeField] Shader _offsetShader;

        private Material _offsetMaterial;

        #endregion

        #region Private members

        [SerializeField] Material _shaderSheet;
        Vector2 _previousInput;

        static class Kernels
        {
            public const int Advect = 0;
            public const int Force = 1;
            public const int PSetup = 2;
            public const int PFinish = 3;
            public const int Jacobi1 = 4;
            public const int Jacobi2 = 5;
        }

        int ThreadCountX { get { return (_resolution + 7) / 8; } }
        int ThreadCountY { get { return (_resolution + 7) / 8; } }

        int ResolutionX { get { return ThreadCountX * 8; } }
        int ResolutionY { get { return ThreadCountY * 8; } }

        // Vector field buffers
        static class VFB
        {
            public static RenderTexture V1;
            public static RenderTexture V2;
            public static RenderTexture V3;
            public static RenderTexture P1;
            public static RenderTexture P2;
        }

        // Color buffers (for double buffering)
        RenderTexture _colorRT1;
        RenderTexture _colorRT2;

        RenderTexture AllocateBuffer(int componentCount, int width = 0, int height = 0)
        {
            var format = RenderTextureFormat.ARGBHalf;
            if (componentCount == 1) format = RenderTextureFormat.RHalf;
            if (componentCount == 2) format = RenderTextureFormat.RGHalf;

            if (width == 0) width = ResolutionX;
            if (height == 0) height = ResolutionY;

            var rt = new RenderTexture(width, height, 0, format);
            rt.enableRandomWrite = true;
            rt.Create();
            return rt;
        }

        #endregion

        #region MonoBehaviour implementation

        void OnValidate()
        {
            _resolution = Mathf.Max(_resolution, 8);
        }

        void Start()
        {
            _offsetMaterial = new Material(_offsetShader);

            if (!_shaderSheet)
                _shaderSheet = new Material(_shader);

            VFB.V1 = AllocateBuffer(2);
            VFB.V2 = AllocateBuffer(2);
            VFB.V3 = AllocateBuffer(2);
            VFB.P1 = AllocateBuffer(1);
            VFB.P2 = AllocateBuffer(1);

            _colorRT1 = AllocateBuffer(4, ResolutionX, ResolutionY);
            _colorRT2 = AllocateBuffer(4, ResolutionX, ResolutionY);

            Graphics.Blit(_initial, _colorRT1);


#if UNITY_IOS
            Application.targetFrameRate = 60;
#endif
        }

        void OnDestroy()
        {
            Destroy(_shaderSheet);

            Destroy(VFB.V1);
            Destroy(VFB.V2);
            Destroy(VFB.V3);
            Destroy(VFB.P1);
            Destroy(VFB.P2);

            Destroy(_colorRT1);
            Destroy(_colorRT2);
        }

        void Update()
        {
            var dt = Time.deltaTime;
            var dx = 1.0f / ResolutionY;

            float px = _player.transform.position.x;
            float pz = _player.transform.position.z;
            float tx = _target.transform.position.x;
            float tz = _target.transform.position.z;

            float playerX = (tx - px) / 10f;
            float playerZ = (tz - pz) / 10f;

            var input = new Vector2(
                playerX,
                playerZ
            );


            if (_player == null) return;
            if (_compute == null) return;

            // Common variables
            _compute.SetFloat("Time", Time.time);
            _compute.SetFloat("DeltaTime", dt);

            // Advection
            _compute.SetTexture(Kernels.Advect, "U_in", VFB.V1);
            _compute.SetTexture(Kernels.Advect, "W_out", VFB.V2);
            _compute.Dispatch(Kernels.Advect, ThreadCountX, ThreadCountY, 1);

            // Diffuse setup
            var dif_alpha = dx * dx / (_viscosity * dt);
            _compute.SetFloat("Alpha", dif_alpha);
            _compute.SetFloat("Beta", 4 + dif_alpha);
            Graphics.CopyTexture(VFB.V2, VFB.V1);
            _compute.SetTexture(Kernels.Jacobi2, "B2_in", VFB.V1);

            // Jacobi iteration
            for (var i = 0; i < 20; i++)
            {
                _compute.SetTexture(Kernels.Jacobi2, "X2_in", VFB.V2);
                _compute.SetTexture(Kernels.Jacobi2, "X2_out", VFB.V3);
                _compute.Dispatch(Kernels.Jacobi2, ThreadCountX, ThreadCountY, 1);

                _compute.SetTexture(Kernels.Jacobi2, "X2_in", VFB.V3);
                _compute.SetTexture(Kernels.Jacobi2, "X2_out", VFB.V2);
                _compute.Dispatch(Kernels.Jacobi2, ThreadCountX, ThreadCountY, 1);
            }

            // Add external force
            _compute.SetVector("ForceOrigin", input);
            _compute.SetFloat("ForceExponent", _exponent);
            _compute.SetTexture(Kernels.Force, "W_in", VFB.V2);
            _compute.SetTexture(Kernels.Force, "W_out", VFB.V3);

            //if (Input.GetMouseButton(1))
            //    // Random push
            //    _compute.SetVector("ForceVector", Random.insideUnitCircle * _force * 0.025f);
            //else if (Input.GetMouseButton(0))
            //    // Mouse drag
            //    _compute.SetVector("ForceVector", (input - _previousInput) * _force);
            //else
            _compute.SetVector("ForceVector", (input - _previousInput) * _force);
            //_compute.SetVector("ForceVector", Random.insideUnitCircle * _force * 0.025f);

            _compute.Dispatch(Kernels.Force, ThreadCountX, ThreadCountY, 1);

            // Projection setup
            _compute.SetTexture(Kernels.PSetup, "W_in", VFB.V3);
            _compute.SetTexture(Kernels.PSetup, "DivW_out", VFB.V2);
            _compute.SetTexture(Kernels.PSetup, "P_out", VFB.P1);
            _compute.Dispatch(Kernels.PSetup, ThreadCountX, ThreadCountY, 1);

            // Jacobi iteration
            _compute.SetFloat("Alpha", -dx * dx);
            _compute.SetFloat("Beta", 4);
            _compute.SetTexture(Kernels.Jacobi1, "B1_in", VFB.V2);

            for (var i = 0; i < 20; i++)
            {
                _compute.SetTexture(Kernels.Jacobi1, "X1_in", VFB.P1);
                _compute.SetTexture(Kernels.Jacobi1, "X1_out", VFB.P2);
                _compute.Dispatch(Kernels.Jacobi1, ThreadCountX, ThreadCountY, 1);

                _compute.SetTexture(Kernels.Jacobi1, "X1_in", VFB.P2);
                _compute.SetTexture(Kernels.Jacobi1, "X1_out", VFB.P1);
                _compute.Dispatch(Kernels.Jacobi1, ThreadCountX, ThreadCountY, 1);
            }

            // Projection finish
            _compute.SetTexture(Kernels.PFinish, "W_in", VFB.V3);
            _compute.SetTexture(Kernels.PFinish, "P_in", VFB.P1);
            _compute.SetTexture(Kernels.PFinish, "U_out", VFB.V1);
            _compute.Dispatch(Kernels.PFinish, ThreadCountX, ThreadCountY, 1);

            // Apply the velocity field to the color buffer.
            //var offs = Vector2.one * (Input.GetMouseButton(1) ? 0 : 1e+7f);
            //_shaderSheet.SetVector("_ForceOrigin", input + offs);
            _shaderSheet.SetVector("_ForceOrigin", input);
            _shaderSheet.SetFloat("_ForceExponent", _exponent);
            _shaderSheet.SetTexture("_MainTex", _colorRT1);
            _shaderSheet.SetTexture("_VelocityField", VFB.V1);
            Graphics.Blit(_colorRT1, _colorRT2, _shaderSheet, 0);



            // Swap the color buffers.
            var temp = _colorRT1;
            _colorRT1 = _colorRT2;
            _colorRT2 = temp;

            _previousInput = input;
        }

        public Texture GetVelocityField()
        {
            return VFB.V1;
        }

        public void ResetVelocityField(Vector2 offset)
        {
            RenderTexture color = AllocateBuffer(4, _colorRT1.width, _colorRT1.height);
            Graphics.Blit(_colorRT1, color);
            RenderTexture velocityField = AllocateBuffer(2, VFB.V1.width, VFB.V1.height);
            Graphics.Blit(VFB.V1, velocityField);

            float px = _player.transform.position.x;
            float pz = _player.transform.position.z;
            float tx = _target.transform.position.x;
            float tz = _target.transform.position.z;

            float playerX = (tx - px) / 10f;
            float playerZ = (tz - pz) / 10f;

            var input = new Vector2(
                playerX,
                playerZ
            );
            _previousInput = input;

            _offsetMaterial.SetVector("_Offset", new Vector4(offset.x, offset.y, 0, 0));
            Graphics.Blit(velocityField, VFB.V1, _offsetMaterial);
            Graphics.Blit(color, _colorRT1, _offsetMaterial);

            Destroy(velocityField);
            Destroy(color);
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(_colorRT1, destination, _shaderSheet, 1);
        }

        #endregion
    }
}
