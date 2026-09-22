using Unity.XR.CoreUtils;
using UnityEngine.Assertions;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    /// <summary>
    /// Permite elegir movimiento relativo a la cabeza o a los mandos.
    /// Calcula el desplazamiento utilizando la vertical actual
    /// del XR Origin, incluso cuando está inclinado o boca abajo.
    /// </summary>
    public class DynamicMoveProvider : ContinuousMoveProvider
    {
        public enum MovementDirection
        {
            HeadRelative,
            HandRelative,
        }

        [Space]
        [Header("Movement Direction")]
        [SerializeField]
        [Tooltip(
            "Referencia de la cabeza para el movimiento. " +
            "Si no se asigna, se utiliza la cámara del XR Origin."
        )]
        private Transform m_HeadTransform;

        public Transform headTransform
        {
            get => m_HeadTransform;
            set => m_HeadTransform = value;
        }

        [SerializeField]
        [Tooltip(
            "Referencia del mando izquierdo para el movimiento " +
            "relativo a ese mando."
        )]
        private Transform m_LeftControllerTransform;

        public Transform leftControllerTransform
        {
            get => m_LeftControllerTransform;
            set => m_LeftControllerTransform = value;
        }

        [SerializeField]
        [Tooltip(
            "Referencia del mando derecho para el movimiento " +
            "relativo a ese mando."
        )]
        private Transform m_RightControllerTransform;

        public Transform rightControllerTransform
        {
            get => m_RightControllerTransform;
            set => m_RightControllerTransform = value;
        }

        [SerializeField]
        [Tooltip(
            "Referencia utilizada para orientar el movimiento " +
            "del joystick izquierdo."
        )]
        private MovementDirection m_LeftHandMovementDirection;

        public MovementDirection leftHandMovementDirection
        {
            get => m_LeftHandMovementDirection;
            set => m_LeftHandMovementDirection = value;
        }

        [SerializeField]
        [Tooltip(
            "Referencia utilizada para orientar el movimiento " +
            "del joystick derecho."
        )]
        private MovementDirection m_RightHandMovementDirection;

        public MovementDirection rightHandMovementDirection
        {
            get => m_RightHandMovementDirection;
            set => m_RightHandMovementDirection = value;
        }

        private Transform m_CombinedTransform;

        private Pose m_LeftMovementPose = Pose.identity;
        private Pose m_RightMovementPose = Pose.identity;

        private Vector3 m_CorrectedInAirVelocity;

        protected override void Awake()
        {
            base.Awake();

            m_CombinedTransform = new GameObject(
                "[Dynamic Move Provider] Combined Forward Source"
            ).transform;

            m_CombinedTransform.SetParent(transform, false);
            m_CombinedTransform.localPosition = Vector3.zero;
            m_CombinedTransform.localRotation = Quaternion.identity;

            forwardSource = m_CombinedTransform;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_CorrectedInAirVelocity = Vector3.zero;
        }

        protected override Vector3 ComputeDesiredMove(Vector2 input)
        {
            if (mediator == null || mediator.xrOrigin == null)
            {
                return Vector3.zero;
            }

            if (input == Vector2.zero)
            {
                return ComputeCorrectedMove(input);
            }

            // Obtiene la cámara si no se ha asignado la cabeza.
            if (m_HeadTransform == null)
            {
                var xrCamera = mediator.xrOrigin.Camera;

                if (xrCamera != null)
                {
                    m_HeadTransform = xrCamera.transform;
                }
            }

            // Referencia para el joystick izquierdo.
            switch (m_LeftHandMovementDirection)
            {
                case MovementDirection.HeadRelative:
                    if (m_HeadTransform != null)
                    {
                        m_LeftMovementPose =
                            m_HeadTransform.GetWorldPose();
                    }

                    break;

                case MovementDirection.HandRelative:
                    if (m_LeftControllerTransform != null)
                    {
                        m_LeftMovementPose =
                            m_LeftControllerTransform.GetWorldPose();
                    }

                    break;

                default:
                    Assert.IsTrue(
                        false,
                        $"Unhandled {nameof(MovementDirection)}=" +
                        $"{m_LeftHandMovementDirection}"
                    );

                    break;
            }

            // Referencia para el joystick derecho.
            switch (m_RightHandMovementDirection)
            {
                case MovementDirection.HeadRelative:
                    if (m_HeadTransform != null)
                    {
                        m_RightMovementPose =
                            m_HeadTransform.GetWorldPose();
                    }

                    break;

                case MovementDirection.HandRelative:
                    if (m_RightControllerTransform != null)
                    {
                        m_RightMovementPose =
                            m_RightControllerTransform.GetWorldPose();
                    }

                    break;

                default:
                    Assert.IsTrue(
                        false,
                        $"Unhandled {nameof(MovementDirection)}=" +
                        $"{m_RightHandMovementDirection}"
                    );

                    break;
            }

            // Combina las referencias según la intensidad
            // de entrada de cada joystick.
            Vector2 leftHandValue = leftHandMoveInput.ReadValue();
            Vector2 rightHandValue = rightHandMoveInput.ReadValue();

            float totalSqrMagnitude =
                leftHandValue.sqrMagnitude +
                rightHandValue.sqrMagnitude;

            float leftHandBlend = 0.5f;

            if (totalSqrMagnitude > Mathf.Epsilon)
            {
                leftHandBlend =
                    leftHandValue.sqrMagnitude / totalSqrMagnitude;
            }

            Vector3 combinedPosition = Vector3.Lerp(
                m_RightMovementPose.position,
                m_LeftMovementPose.position,
                leftHandBlend
            );

            Quaternion combinedRotation = Quaternion.Slerp(
                m_RightMovementPose.rotation,
                m_LeftMovementPose.rotation,
                leftHandBlend
            );

            m_CombinedTransform.SetPositionAndRotation(
                combinedPosition,
                combinedRotation
            );

            return ComputeCorrectedMove(input);
        }

        /// <summary>
        /// Calcula el desplazamiento en coordenadas globales
        /// usando la vertical actual del XR Origin.
        /// </summary>
        private Vector3 ComputeCorrectedMove(Vector2 input)
        {
            if (mediator == null || mediator.xrOrigin == null)
            {
                return Vector3.zero;
            }

            var xrOrigin = mediator.xrOrigin;

            if (xrOrigin.Origin == null)
            {
                return Vector3.zero;
            }

            Transform origin = xrOrigin.Origin.transform;
            Transform source = forwardSource;

            if (source == null)
            {
                source = xrOrigin.Camera != null
                    ? xrOrigin.Camera.transform
                    : origin;
            }

            // Limita el movimiento diagonal y conserva
            // la intensidad analógica del joystick.
            Vector3 inputMove = Vector3.ClampMagnitude(
                new Vector3(
                    enableStrafe ? input.x : 0f,
                    0f,
                    input.y
                ),
                1f
            );

            // Conserva el comportamiento de control en el aire
            // del ContinuousMoveProvider proporcionado.
            var gravity = gravityProvider;

            if (
                enableFly ||
                inAirControlModifier >= 1f ||
                gravity == null ||
                !gravity.enabled ||
                !gravity.useGravity ||
                gravity.isGrounded
            )
            {
                m_CorrectedInAirVelocity = inputMove;
            }
            else
            {
                m_CorrectedInAirVelocity +=
                    Time.deltaTime *
                    inAirControlModifier *
                    10f *
                    (inputMove - m_CorrectedInAirVelocity);

                if (
                    m_CorrectedInAirVelocity.sqrMagnitude
                    <= 0.0001f
                )
                {
                    m_CorrectedInAirVelocity = Vector3.zero;
                }
            }

            float speedFactor =
                moveSpeed *
                Time.deltaTime *
                origin.localScale.x;

            // Movimiento libre si se activa el modo de vuelo.
            if (enableFly)
            {
                return (
                    source.right * inputMove.x +
                    source.forward * inputMove.z
                ) * speedFactor;
            }

            Vector3 up = origin.up;
            Vector3 inputForward = source.forward;

            // Conserva la alternativa del proveedor original
            // cuando la dirección de mirada es paralela
            // a la vertical del jugador.
            if (
                Mathf.Approximately(
                    Mathf.Abs(Vector3.Dot(inputForward, up)),
                    1f
                )
            )
            {
                inputForward = -source.up;
            }

            // Proyecta el avance sobre el plano del jugador.
            Vector3 forward = Vector3.ProjectOnPlane(
                inputForward,
                up
            );

            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = origin.forward;
            }

            forward.Normalize();

            // Calcula la derecha respecto al arriba actual,
            // también cuando el jugador está boca abajo.
            Vector3 right = Vector3.Cross(
                up,
                forward
            ).normalized;

            // Las direcciones ya están en coordenadas globales:
            // no se aplica TransformDirection de nuevo.
            return (
                right * m_CorrectedInAirVelocity.x +
                forward * m_CorrectedInAirVelocity.z
            ) * speedFactor;
        }
    }
}