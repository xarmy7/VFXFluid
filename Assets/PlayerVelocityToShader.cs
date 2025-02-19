using UnityEngine;

namespace StableFluids
{
    public class PlayerVelocityToShader : MonoBehaviour
    {
        [SerializeField] private ComputeShader _computeShader;
        [SerializeField] private Fluid _fluid;

        private Vector3 _previousPosition;
        [SerializeField] private Vector3 _velocity;

        void Start()
        {
            _previousPosition = transform.position;
        }

        void Update()
        {
            _velocity = (transform.position - _previousPosition) / Time.deltaTime;
            _previousPosition = transform.position;

            if (_computeShader != null)
            {
                _computeShader.SetVector("PlayerVelocity", new Vector2(_velocity.x, _velocity.z));
            }

            //if (_fluid != null)
            //{
            //    _fluid.SetPlayerVelocity(_velocity);
            //}
        }
    }
}
