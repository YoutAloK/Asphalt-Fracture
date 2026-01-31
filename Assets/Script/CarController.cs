using UnityEngine;
using Fusion;

public class CarController : NetworkBehaviour
{
    [Header("Настройки управления")]
    public float motorForce = 1500f;
    public float steeringAngle = 30f;
    public float brakeForce = 5000f;
    
    [Header("Коллайдеры")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;
    
    [Header("Меши")]
    public Transform frontLeftWheelMesh;
    public Transform frontRightWheelMesh;
    public Transform rearLeftWheelMesh;
    public Transform rearRightWheelMesh;
    
    [Header("Камера")]
    [SerializeField] private bool autoCreateCamera = true;

    // Сетевые переменные для синхронизации
    [Networked] private float HorizontalInput { get; set; }
    [Networked] private float VerticalInput { get; set; }
    [Networked] private NetworkBool IsBraking { get; set; }

    private Rigidbody rb;
    private CarCameraController cameraController;
    
    // Буфер для сглаживания input (предотвращает рывки)
    private float smoothHorizontal = 0f;
    private float smoothVertical = 0f;
    private const float inputSmoothTime = 0.05f;
    
    public override void Spawned()
    {
        Debug.Log($"=== CarController.Spawned() ===");
        Debug.Log($"Car: {gameObject.name}");
        Debug.Log($"InputAuthority: {Object.InputAuthority}");
        Debug.Log($"HasInputAuthority: {Object.HasInputAuthority}");
        Debug.Log($"LocalPlayer: {Runner.LocalPlayer}");
        
        rb = GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            // Оптимизированные настройки Rigidbody для сетевой игры
            rb.centerOfMass = new Vector3(0, -0.5f, 0);
            rb.mass = 1500f;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.5f;
            
            // КРИТИЧНО: Fusion сам управляет интерполяцией
            rb.interpolation = RigidbodyInterpolation.None;
            
            // Для локального игрока - улучшенная детекция коллизий
            rb.collisionDetectionMode = Object.HasInputAuthority 
                ? CollisionDetectionMode.ContinuousDynamic 
                : CollisionDetectionMode.Discrete;
            
            Debug.Log($"Rigidbody configured: mass={rb.mass}, interpolation=None");
        }
        
        ConfigureWheels();
        
        // Визуальная индикация: зеленый = ваша машина, синий = противник
        var renderers = GetComponentsInChildren<Renderer>();
        Color carColor = Object.HasInputAuthority ? Color.green : new Color(0.3f, 0.5f, 1f);
        
        foreach (var renderer in renderers)
        {
            if (renderer.material != null)
            {
                renderer.material.color = carColor;
            }
        }
        
        Debug.Log($"Car color set to: {(Object.HasInputAuthority ? "GREEN (yours)" : "BLUE (opponent)")}");
        
        // Создание камеры только для локального игрока
        if (Object.HasInputAuthority && autoCreateCamera)
        {
            CreateCamera();
        }
    }
    
    private void CreateCamera()
    {
        cameraController = GetComponent<CarCameraController>();
        
        if (cameraController == null)
        {
            cameraController = gameObject.AddComponent<CarCameraController>();
            Debug.Log("✓ Camera controller added to car");
        }
    }
    
    private void ConfigureWheels()
    {
        ConfigureSingleWheel(frontLeftWheel, true);
        ConfigureSingleWheel(frontRightWheel, true);
        ConfigureSingleWheel(rearLeftWheel, false);
        ConfigureSingleWheel(rearRightWheel, false);
    }
    
    private void ConfigureSingleWheel(WheelCollider wheel, bool isFrontWheel)
    {
        if (wheel == null) return;
        
        wheel.mass = 20f;
        wheel.radius = 0.03f;
        wheel.suspensionDistance = 0.2f;
        
        WheelFrictionCurve forwardFriction = wheel.forwardFriction;
        forwardFriction.stiffness = 1.5f;
        wheel.forwardFriction = forwardFriction;
        
        WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
        sidewaysFriction.stiffness = 1.5f;
        wheel.sidewaysFriction = sidewaysFriction;
        
        JointSpring spring = wheel.suspensionSpring;
        spring.spring = 30000f;
        spring.damper = 4500f;
        spring.targetPosition = 0.5f;
        wheel.suspensionSpring = spring;
        
        wheel.wheelDampingRate = 0.25f;
        wheel.forceAppPointDistance = 0f;
    }

    public override void FixedUpdateNetwork()
    {
        // Получаем input только от игрока с authority
        if (GetInput(out NetworkInputData input))
        {
            // Плавное сглаживание input для предотвращения рывков
            smoothHorizontal = Mathf.Lerp(smoothHorizontal, input.Horizontal, inputSmoothTime);
            smoothVertical = Mathf.Lerp(smoothVertical, input.Vertical, inputSmoothTime);
            
            HorizontalInput = smoothHorizontal;
            VerticalInput = smoothVertical;
            IsBraking = input.IsBraking;
        }

        // Применяем физику - Fusion автоматически синхронизирует Rigidbody
        ApplySteering();
        ApplyDriveAndBraking();
        ApplyAntiRoll();
    }
    
    private void ApplySteering()
    {
        float steer = steeringAngle * HorizontalInput;
        frontLeftWheel.steerAngle = steer;
        frontRightWheel.steerAngle = steer;
    }
    
    private void ApplyDriveAndBraking()
    {
        float currentSpeedRpm = rearLeftWheel.rpm;

        // Принудительное торможение (пробел)
        if (IsBraking)
        {
            ApplyMotorTorque(0);
            ApplyBrakeTorque(brakeForce);
            return;
        }

        if (Mathf.Abs(VerticalInput) > 0.1f)
        {
            bool isReversing = (VerticalInput < 0 && currentSpeedRpm > 1);
            bool isBrakingForward = (VerticalInput > 0 && currentSpeedRpm < -1);

            if (isReversing || isBrakingForward)
            {
                ApplyMotorTorque(0);
                ApplyBrakeTorque(brakeForce);
            }
            else
            {
                ApplyMotorTorque(motorForce * VerticalInput);
                ApplyBrakeTorque(0);
            }
        }
        else
        {
            // Автоторможение когда нет input
            ApplyMotorTorque(0);
            ApplyBrakeTorque(100f);
        }
    }

    private void ApplyMotorTorque(float torque)
    {
        rearLeftWheel.motorTorque = torque;
        rearRightWheel.motorTorque = torque;
    }

    private void ApplyBrakeTorque(float brake)
    {
        frontLeftWheel.brakeTorque = brake;
        frontRightWheel.brakeTorque = brake;
        rearLeftWheel.brakeTorque = brake;
        rearRightWheel.brakeTorque = brake;
    }
    
    private void ApplyAntiRoll()
    {
        if (rb == null) return;
        
        ApplyAntiRollToAxle(frontLeftWheel, frontRightWheel);
        ApplyAntiRollToAxle(rearLeftWheel, rearRightWheel);
    }
    
    private void ApplyAntiRollToAxle(WheelCollider leftWheel, WheelCollider rightWheel)
    {
        if (leftWheel == null || rightWheel == null) return;
        
        float antiRoll = 5000f;
        
        WheelHit hit;
        float travelL = 1.0f;
        float travelR = 1.0f;
        
        bool groundedL = leftWheel.GetGroundHit(out hit);
        if (groundedL)
            travelL = (-leftWheel.transform.InverseTransformPoint(hit.point).y - leftWheel.radius) / leftWheel.suspensionDistance;
        
        bool groundedR = rightWheel.GetGroundHit(out hit);
        if (groundedR)
            travelR = (-rightWheel.transform.InverseTransformPoint(hit.point).y - rightWheel.radius) / rightWheel.suspensionDistance;
        
        float antiRollForce = (travelL - travelR) * antiRoll;
        
        if (groundedL)
            rb.AddForceAtPosition(leftWheel.transform.up * -antiRollForce, leftWheel.transform.position);
        if (groundedR)
            rb.AddForceAtPosition(rightWheel.transform.up * antiRollForce, rightWheel.transform.position);
    }

    public override void Render()
    {
        UpdateWheelVisuals();
    }
    
    private void UpdateWheelVisuals()
    {
        UpdateSingleWheel(frontLeftWheel, frontLeftWheelMesh);
        UpdateSingleWheel(frontRightWheel, frontRightWheelMesh);
        UpdateSingleWheel(rearLeftWheel, rearLeftWheelMesh);
        UpdateSingleWheel(rearRightWheel, rearRightWheelMesh);
    }
    
    private void UpdateSingleWheel(WheelCollider wc, Transform tr)
    {
        if (tr == null || wc == null) return;
        wc.GetWorldPose(out Vector3 pos, out Quaternion rot);
        tr.SetPositionAndRotation(pos, rot);
    }
    
    public CarCameraController GetCameraController()
    {
        return cameraController;
    }
}