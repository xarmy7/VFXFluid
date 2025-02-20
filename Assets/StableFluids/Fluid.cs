// StableFluids - A GPU implementation of Jos Stam's Stable Fluids on Unity
// https://github.com/keijiro/StableFluids

using System;
using UnityEngine;

namespace StableFluids
{
    public class Fluid : MonoBehaviour
    {
        #region Editable attributes

        [SerializeField] int _resolution = 512;
        [SerializeField] float _viscosity = 1e-6f;
        [Obsolete, SerializeField] float _force = 300;
        [Obsolete, SerializeField] float _exponent = 200;
        [Obsolete, SerializeField] float _radiusScale = 2.2f;
        [SerializeField] float _velocityScale = 1.4f;
        [SerializeField] Texture2D _initial;

        // colliders
        [SerializeField] CharacterController characterController;
        [SerializeField] Collider[] colliders;

        [SerializeField] FollowObjectGrid grid;

        #endregion

        #region Internal resources

        [SerializeField, HideInInspector] ComputeShader _compute;
        [SerializeField, HideInInspector] Shader _shader;

        [SerializeField] private Shader _offsetShader;

        private Material _offsetMaterial;

        [HideInInspector] public float planeScale = 0.1f;

        #endregion

        #region Private members

        [SerializeField] private Material _shaderSheet;
        [Obsolete] private Vector2 _previousInput;

        private bool MaterialWasSetInEditor = false;

        [SerializeField] private bool DisplayVelocityFieldTex = true;

        static class Kernels
        {
            public const int Advect = 0;
            public const int Force = 1;
            public const int PSetup = 2;
            public const int PFinish = 3;
            public const int Jacobi1 = 4;
            public const int Jacobi2 = 5;
            public const int ForceNoCollision = 6;
        }

        int ThreadCountX { get { return (_resolution + 7) / 8; } }
        int ThreadCountY { get { return (_resolution + 7) / 8; } }

        public int ResolutionX { get { return ThreadCountX * 8; } }
        public int ResolutionY { get { return ThreadCountY * 8; } }

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

        struct FluidInputData
        {
            public Vector2 position;
            public Vector2 velocity;
            public float radius;
        }

        static readonly FluidInputData defaultData = new FluidInputData()
        {
            position = Vector2.zero,
            velocity = Vector2.zero,
            radius = 0f
        };

        GraphicsBuffer _inputBuffer;
        FluidInputData[] _inputBufferData;

        Vector3[] _previousPositions;

        void InitializeInputBuffer()
        {
            if (_inputBuffer == null)
            {
                _inputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, colliders.Length + 1, sizeof(float) * 5);
                _inputBufferData = new FluidInputData[colliders.Length + 1];
                _previousPositions = new Vector3[colliders.Length + 1];
            }

            UpdateInputBuffer(0f);
        }


        void OnValidate()
        {
            _resolution = Mathf.Max(_resolution, 8);
        }

        void Start()
        {
            planeScale = transform.localScale.x * 0.1f;

            _offsetMaterial = new Material(_offsetShader);

            if (!_shaderSheet)
                _shaderSheet = new Material(_shader);
            else
                MaterialWasSetInEditor = true;

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

            InitializeInputBuffer();
        }

        void OnDestroy()
        {
            if (!MaterialWasSetInEditor)
                Destroy(_shaderSheet);

            Destroy(VFB.V1);
            Destroy(VFB.V2);
            Destroy(VFB.V3);
            Destroy(VFB.P1);
            Destroy(VFB.P2);

            Destroy(_colorRT1);
            Destroy(_colorRT2);
        }

        [Obsolete("Input with player gameobject only")]
        Vector2 UpdateInput()
        {
            // player
            float px = characterController.transform.position.x;
            float pz = characterController.transform.position.z;
            // target (self)
            float tx = transform.position.x;
            float tz = transform.position.z;

            float playerX = (tx - px) * planeScale;
            float playerZ = (tz - pz) * planeScale;

            var input = new Vector2(
                playerX,
                playerZ
            );

            return input;
        }

        void LateUpdate()
        {
            var dt = Time.deltaTime;
            var dx = 1.0f / ResolutionY;

            var input = UpdateInput();

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

            UpdateInputBuffer(dt);

            // Add external force (legacy technique)
            _compute.SetVector("ForceOrigin", input);
            _compute.SetFloat("ForceExponent", _exponent);
            _compute.SetVector("ForceVector", (input - _previousInput) * _force);
            // Add external force
            _compute.SetBuffer(Kernels.Force, "fluidInput", _inputBuffer);
            _compute.SetInt("FluidInputCount", _inputBufferData.Length);
            _compute.SetTexture(Kernels.Force, "W_in", VFB.V2);
            _compute.SetTexture(Kernels.Force, "W_out", VFB.V3);
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
            _shaderSheet.SetTexture("_MainTex", DisplayVelocityFieldTex ? VFB.V1 : _colorRT1);
            _shaderSheet.SetTexture("_VelocityField", VFB.V1);
            Graphics.Blit(_colorRT1, _colorRT2, _shaderSheet, 0);

            // Swap the color buffers.
            var temp = _colorRT1;
            _colorRT1 = _colorRT2;
            _colorRT2 = temp;

            _previousInput = input;
        }

        void UpdateData(int index, Collider collider, float deltaTime)
        {
            FluidInputData d = _inputBufferData[index];
            Vector3 position = collider.transform.position;

            if (deltaTime == 0f)
                _previousPositions[index] = position;

            // Compute position in 2D
            Vector3 pos3d = (transform.position - position) * planeScale;
            Vector2 pos2d = grid.GetCellPosition2D(position - transform.position);
            pos2d = new Vector2(pos3d.x, pos3d.z);

            // Compute velocity in 2D
            Vector3 vel3d = _previousPositions[index] - position;
            Vector2 vel2d = new Vector2(vel3d.x, vel3d.z);
            _previousPositions[index] = position;

            // Compute radius in 2D
            float radius = 0f;
            if (collider is CharacterController)
                radius = ((CharacterController)collider).radius;
            else if (collider is SphereCollider)
                radius = ((SphereCollider)collider).radius * collider.transform.localScale.x;

            d.radius = radius * planeScale;

            // Only store velocity for update pass
            if (deltaTime > 0)
                d.velocity = vel2d * _velocityScale;
            else
                d.velocity = Vector2.zero;

            // Store position
            d.position = pos2d;

            _inputBufferData[index] = d;
        }

        public Texture GetVelocityField()
        {
            return VFB.V1;
        }

        void UpdateInputBuffer(float deltaTime)
        {
            if (characterController == null)
                _inputBufferData[0] = defaultData;
            else
                UpdateData(0, characterController, deltaTime);

            for (int i = 0; i < colliders.Length; i++)
            {
                UpdateData(i + 1, colliders[i], deltaTime);
            }

            _inputBuffer.SetData(_inputBufferData);
        }

        public void SnapVelocityField(Vector2 offset)
        {
            RenderTexture color = AllocateBuffer(4, _colorRT1.width, _colorRT1.height);
            Graphics.Blit(_colorRT1, color);
            RenderTexture velocityField = AllocateBuffer(2, VFB.V1.width, VFB.V1.height);
            Graphics.Blit(VFB.V1, velocityField);

            _previousInput = UpdateInput();

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
