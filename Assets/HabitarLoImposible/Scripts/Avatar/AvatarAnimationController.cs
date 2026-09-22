using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla las animaciones de movimiento y salto del avatar
/// mediante el joystick, el movimiento físico del visor y la
/// velocidad aplicada dentro del espacio hiperbólico.
///
/// También reproduce un sonido continuo de pasos mientras
/// el avatar está caminando.
/// </summary>
public class AvatarAnimationController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private Transform xrCamera;

    [SerializeField]
    private Transform xrOrigin;

    [SerializeField]
    private HyperbolicMovementController hyperbolicMovementController;

    [Header("Input Actions")]
    [SerializeField]
    private InputActionReference moveAction;

    [SerializeField]
    private InputActionReference jumpAction;

    [Header("Detección de movimiento físico")]
    [SerializeField]
    private float physicalMovementThreshold = 0.15f;

    [SerializeField]
    private float fullWalkingSpeed = 1.2f;

    [Header("Suavizado")]
    [SerializeField]
    private float speedSmoothTime = 0.08f;

    [Header("Velocidad de la animación")]
    [SerializeField]
    private float minimumMoveAnimationSpeed = 0.35f;

    [SerializeField]
    private float maximumMoveAnimationSpeed = 1.5f;

    [Header("Sonido continuo de pasos")]
    [SerializeField]
    private AudioSource footstepAudioSource;

    [SerializeField]
    private AudioClip walkingSound;

    [Tooltip("Movimiento mínimo para activar el sonido.")]
    [SerializeField]
    private float minimumSpeedForFootsteps = 0.05f;

    [SerializeField]
    private float minimumFootstepVolume = 0.4f;

    [SerializeField]
    private float maximumFootstepVolume = 1f;

    [Tooltip("Velocidad con la que aparece y desaparece el sonido.")]
    [SerializeField]
    private float footstepVolumeSpeed = 8f;

    [Tooltip("Pitch mínimo del sonido cuando se camina despacio.")]
    [SerializeField]
    private float minimumFootstepPitch = 0.8f;

    [Tooltip("Pitch máximo del sonido cuando se camina rápido.")]
    [SerializeField]
    private float maximumFootstepPitch = 1.2f;

    private static readonly int SpeedParameter = Animator.StringToHash("Speed");

    private static readonly int JumpParameter = Animator.StringToHash("Jump");

    private static readonly int MoveAnimationSpeedParameter = Animator.StringToHash("MoveAnimationSpeed");

    private Vector3 previousCameraPosition;

    private float currentSpeed;
    private float speedSmoothVelocity;
    private float currentMoveAnimationSpeed = 1f;
    private float currentFootstepVolume;

    /// <summary>
    /// Guarda la posición inicial de la cámara y configura el AudioSource utilizado para los pasos.
    /// </summary>
    private void Start()
    {
        if (xrCamera != null)
        {
            previousCameraPosition = xrCamera.position;
        }

        if (footstepAudioSource != null)
        {
            footstepAudioSource.clip = walkingSound;
            footstepAudioSource.loop = true;
            footstepAudioSource.playOnAwake = false;
            footstepAudioSource.volume = 0f;
        }
    }

    /// <summary>
    /// Activa las acciones de movimiento y salto, y registra el método encargado de iniciar la animación de salto.
    /// </summary>
    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.action.Enable();
        }

        if (jumpAction != null)
        {
            jumpAction.action.performed += OnJumpPerformed;

            jumpAction.action.Enable();
        }
    }

    /// <summary>
    /// Elimina las suscripciones y detiene el sonido cuando se desactiva el componente.
    /// </summary>
    private void OnDisable()
    {
        if (jumpAction != null)
        {
            jumpAction.action.performed -= OnJumpPerformed;

            jumpAction.action.Disable();
        }

        if (moveAction != null)
        {
            moveAction.action.Disable();
        }

        if (footstepAudioSource != null)
        {
            footstepAudioSource.Stop();
            footstepAudioSource.volume = 0f;
        }

        currentFootstepVolume = 0f;
    }

    /// <summary>
    /// Actualiza la animación y el sonido del avatar en cada fotograma.
    /// </summary>
    private void Update()
    {
        if (animator == null || xrCamera == null || xrOrigin == null)
        {
            StopFootstepSound();
            return;
        }

        UpdateMovementAnimation();
        UpdateFootstepSound();
    }

    /// <summary>
    /// Calcula la velocidad del avatar y actualiza los parámetros de movimiento del Animator.
    /// </summary>
    private void UpdateMovementAnimation()
    {
        float inputSpeed = GetInputSpeed();
        float physicalSpeed = GetPhysicalSpeed();

        float targetSpeed = Mathf.Max(inputSpeed, physicalSpeed);

        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, speedSmoothTime);

        if (targetSpeed == 0f && currentSpeed < 0.01f)
        {
            currentSpeed = 0f;
        }

        animator.SetFloat(SpeedParameter, currentSpeed);

        currentMoveAnimationSpeed = 1f;

        if (hyperbolicMovementController != null)
        {
            currentMoveAnimationSpeed = hyperbolicMovementController.AnimationMovementFactor;
        }

        currentMoveAnimationSpeed = Mathf.Clamp(currentMoveAnimationSpeed, minimumMoveAnimationSpeed, maximumMoveAnimationSpeed);

        animator.SetFloat(MoveAnimationSpeedParameter, currentMoveAnimationSpeed);
    }

    /// <summary>
    /// Reproduce el sonido de pasos en bucle mientras el avatar camina y adapta su ritmo a la animación.
    /// </summary>
    private void UpdateFootstepSound()
    {
        if (footstepAudioSource == null || walkingSound == null)
        {
            return;
        }

        bool isWalking = currentSpeed >= minimumSpeedForFootsteps;

        if (isWalking)
        {
            if (footstepAudioSource.clip != walkingSound)
            {
                footstepAudioSource.clip = walkingSound;
            }

            footstepAudioSource.loop = true;

            float normalizedAnimationSpeed = Mathf.InverseLerp(minimumMoveAnimationSpeed, maximumMoveAnimationSpeed, currentMoveAnimationSpeed);

            footstepAudioSource.pitch = Mathf.Lerp(minimumFootstepPitch, maximumFootstepPitch, normalizedAnimationSpeed);

            float targetVolume = Mathf.Lerp(minimumFootstepVolume, maximumFootstepVolume, Mathf.Clamp01(currentSpeed));

            currentFootstepVolume = Mathf.MoveTowards(currentFootstepVolume, targetVolume, footstepVolumeSpeed * Time.deltaTime);

            footstepAudioSource.volume = currentFootstepVolume;

            if (!footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Play();
            }
        }
        else
        {
            currentFootstepVolume = Mathf.MoveTowards(currentFootstepVolume, 0f, footstepVolumeSpeed * Time.deltaTime);

            footstepAudioSource.volume = currentFootstepVolume;

            if (currentFootstepVolume <= 0.01f && footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Stop();
            }
        }
    }

    /// <summary>
    /// Detiene inmediatamente el sonido de los pasos.
    /// </summary>
    private void StopFootstepSound()
    {
        if (footstepAudioSource == null)
        {
            return;
        }

        footstepAudioSource.Stop();
        footstepAudioSource.volume = 0f;
        currentFootstepVolume = 0f;
    }

    /// <summary>
    /// Obtiene la intensidad del movimiento introducido mediante el joystick.
    /// </summary>
    private float GetInputSpeed()
    {
        if (moveAction == null)
        {
            return 0f;
        }

        Vector2 movementInput = moveAction.action.ReadValue<Vector2>();

        return movementInput.magnitude;
    }

    /// <summary>
    /// Calcula y normaliza la velocidad del visor en el plano perpendicular a la vertical del XR Origin a partir de su desplazamiento global.
    /// </summary>
    private float GetPhysicalSpeed()
    {
        Vector3 displacement = xrCamera.position - previousCameraPosition;

        previousCameraPosition = xrCamera.position;

        Vector3 horizontalDisplacement = Vector3.ProjectOnPlane(displacement, xrOrigin.up);

        float physicalSpeed = horizontalDisplacement.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        if (physicalSpeed < physicalMovementThreshold)
        {
            return 0f;
        }

        return Mathf.Clamp01(physicalSpeed / fullWalkingSpeed);
    }

    /// <summary>
    /// Activa el parámetro de salto del Animator cuando se ejecuta la acción correspondiente.
    /// </summary>
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (animator != null)
        {
            animator.SetTrigger(JumpParameter);
        }
    }
}
