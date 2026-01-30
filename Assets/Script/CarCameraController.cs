using UnityEngine;
using Fusion;
using System;

public class CarCameraController : NetworkBehaviour
{
    public enum CameraMode
    {
        Chase,      // Камера сзади машины (классическая гоночная)
        Hood,       // Камера на капоте
        Orbit       // Орбитальная камера вокруг машины
    }
    
    [Header("Режимы камеры")]
    [SerializeField] private CameraMode defaultMode = CameraMode.Chase;
    [SerializeField] private bool allowCameraShake = true;
    
    [Header("Chase Camera (сзади)")]
    [SerializeField] private Vector3 chaseOffset = new Vector3(0f, 2f, -5f);
    [SerializeField] private float chaseHeight = 1.5f;
    [SerializeField] private float chaseDistance = 6f;
    [SerializeField] private float chaseSmoothSpeed = 8f; // Уменьшено для более быстрого отклика
    [SerializeField] private float chaseRotationSpeed = 8f; // Увеличено
    [SerializeField] private float chaseLookAhead = 3f;
    [SerializeField] private float chaseLateralOffset = 1f;
    [SerializeField] private float chaseMaxSpeedOffset = 3f; // Максимальное смещение при скорости
    [SerializeField] private float chaseSpeedSmoothTime = 0.1f; // Сглаживание смещений
    
    [Header("Hood Camera (на капоте)")]
    [SerializeField] private Vector3 hoodPosition = new Vector3(0f, 0.8f, 1.5f);
    [SerializeField] private Vector3 hoodRotation = new Vector3(10f, 0f, 0f);
    [SerializeField] private float hoodShakeAmount = 0.02f;
    
    [Header("Orbit Camera (орбитальная)")]
    [SerializeField] private float orbitDistance = 8f;
    [SerializeField] private float orbitHeight = 2f;
    [SerializeField] private float orbitSpeed = 30f;
    [SerializeField] private float orbitSmoothness = 5f;
    [SerializeField] private float orbitMinDistance = 3f;
    [SerializeField] private float orbitMaxDistance = 15f;
    
    [Header("Общие настройки")]
    [SerializeField] private float fov = 70f;
    [SerializeField] private float speedFovMultiplier = 0.3f;
    [SerializeField] private float maxFov = 85f;
    [SerializeField] private float cameraShakeAmount = 0.1f;
    [SerializeField] private float cameraShakeFrequency = 10f;
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private float collisionRadius = 0.3f;
    [SerializeField] private float collisionOffsetSpeed = 10f;
    
    [Header("Настройки управления")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float scrollSensitivity = 2f;
    [SerializeField] private bool invertMouseY = false;
    
    // Компоненты
    private Transform target;
    private Camera mainCamera;
    private Rigidbody carRigidbody;
    
    // Переменные состояния
    private CameraMode currentMode;
    private Vector3 currentVelocity;
    private Quaternion currentRotation;
    private float currentFov;
    private float currentOrbitAngle = 0f;
    
    // Ввод
    private float mouseX = 0f;
    private float mouseY = 10f;
    private float desiredDistance;
    private float desiredHeight;
    
    // Сглаживание
    private Vector3 smoothPositionVelocity;
    private float smoothLateralOffset;
    private float smoothVerticalOffset;
    private float smoothLookAhead;
    
    // Столкновения
    private float collisionOffset = 0f;
    private RaycastHit collisionHit;
    
    // Эффекты
    private Vector3 lastCarPosition;
    private float roadBumpIntensity = 0f;
    private Vector3 lastCarVelocity;
    
    // Флаги
    private bool isInitialized = false;
    private bool isLocalPlayer = false;
    
    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
        {
            enabled = false;
            return;
        }
        
        isLocalPlayer = true;
        target = transform;
        carRigidbody = GetComponent<Rigidbody>();
        lastCarPosition = target.position;
        lastCarVelocity = Vector3.zero;
        
        InitializeCamera();
        
        currentMode = defaultMode;
        desiredDistance = chaseDistance;
        desiredHeight = chaseHeight;
        
        // Начальные значения сглаживания
        smoothLateralOffset = 0f;
        smoothVerticalOffset = 0f;
        smoothLookAhead = chaseLookAhead;
        
        // Настройки ввода
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        isInitialized = true;
        
        Debug.Log($"Racing camera controller initialized. Mode: {currentMode}");
    }
    
    private void InitializeCamera()
    {
        // Ищем существующую камеру или создаём новую
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject camObj = new GameObject("RacingCamera");
            camObj.tag = "MainCamera";
            mainCamera = camObj.AddComponent<Camera>();
            
            // Добавляем компоненты
            if (FindObjectOfType<AudioListener>() == null)
            {
                camObj.AddComponent<AudioListener>();
            }
        }
        
        // Настройки камеры
        mainCamera.fieldOfView = fov;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 500f;
        currentFov = fov;
        
        // Позиционируем камеру
        ResetCameraPosition();
    }
    
    private void OnDestroy()
    {
        if (isLocalPlayer)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
    
    private void Update()
    {
        if (!isLocalPlayer || !isInitialized) return;
        
        HandleInput();
        UpdateFieldOfView();
    }
    
    private void FixedUpdate()
    {
        if (!isLocalPlayer || !isInitialized) return;
        
        UpdateRoadBumps();
        UpdateCameraEffects();
    }
    
    private void LateUpdate()
    {
        if (!isLocalPlayer || !isInitialized || mainCamera == null || target == null) return;
        
        switch (currentMode)
        {
            case CameraMode.Chase:
                UpdateChaseCamera();
                break;
                
            case CameraMode.Hood:
                UpdateHoodCamera();
                break;
                
            case CameraMode.Orbit:
                UpdateOrbitCamera();
                break;
        }
        
        HandleCollision();
    }
    
    private void HandleInput()
    {
        // Переключение режимов камеры
        if (Input.GetKeyDown(KeyCode.C))
        {
            CycleCameraMode();
        }
        
        if (Input.GetKeyDown(KeyCode.V))
        {
            ToggleCameraShake();
        }
        
        // Управление для Chase и Orbit режимов
        if (currentMode == CameraMode.Chase || currentMode == CameraMode.Orbit)
        {
            // Управление мышью
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                mouseX += Input.GetAxis("Mouse X") * mouseSensitivity;
                
                float mouseYInput = Input.GetAxis("Mouse Y") * mouseSensitivity;
                if (invertMouseY) mouseYInput = -mouseYInput;
                
                mouseY += mouseYInput;
                mouseY = Mathf.Clamp(mouseY, -80f, 80f);
            }
            
            // Регулировка дистанции колесиком мыши
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                float minDist = currentMode == CameraMode.Chase ? 2f : orbitMinDistance;
                float maxDist = currentMode == CameraMode.Chase ? 20f : orbitMaxDistance;
                desiredDistance = Mathf.Clamp(
                    desiredDistance - scroll * scrollSensitivity, 
                    minDist, 
                    maxDist
                );
            }
            
            // Быстрые клавиши для высоты (только для Chase)
            if (currentMode == CameraMode.Chase)
            {
                if (Input.GetKey(KeyCode.PageUp))
                {
                    desiredHeight = Mathf.Clamp(desiredHeight + 0.1f, 0.5f, 10f);
                }
                else if (Input.GetKey(KeyCode.PageDown))
                {
                    desiredHeight = Mathf.Clamp(desiredHeight - 0.1f, 0.5f, 10f);
                }
            }
        }
        
        // Блокировка/разблокировка мыши
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorLock();
        }
        
        // Сброс камеры
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCamera();
        }
    }
    
    private void UpdateChaseCamera()
    {
        if (carRigidbody == null) return;
        
        // Получаем текущую скорость машины
        Vector3 carVelocity = carRigidbody.linearVelocity;
        float speed = carVelocity.magnitude;
        
        // Предсказываем позицию машины (компенсация задержки)
        Vector3 predictedPosition = target.position + carVelocity * 0.05f; // 50ms предсказание
        
        // Рассчитываем факторы на основе движения машины
        Vector3 localVelocity = target.InverseTransformDirection(carVelocity);
        float speedFactor = Mathf.Clamp01(speed / 50f); // Увеличен максимальный порог скорости
        
        float targetTurnFactor = Mathf.Clamp(-localVelocity.x * 0.08f, -1f, 1f); // Уменьшен коэффициент
        float targetAccelerationFactor = Mathf.Clamp(localVelocity.z * 0.03f, -0.5f, 0.5f); // Уменьшен коэффициент
        
        // Сглаживание смещений
        smoothLateralOffset = Mathf.Lerp(
            smoothLateralOffset, 
            targetTurnFactor * chaseLateralOffset * speedFactor, 
            chaseSpeedSmoothTime * Time.deltaTime * 50f
        );
        
        smoothVerticalOffset = Mathf.Lerp(
            smoothVerticalOffset, 
            targetAccelerationFactor * 0.3f, 
            chaseSpeedSmoothTime * Time.deltaTime * 50f
        );
        
        smoothLookAhead = Mathf.Lerp(
            smoothLookAhead, 
            chaseLookAhead + speedFactor * chaseMaxSpeedOffset, 
            chaseSpeedSmoothTime * Time.deltaTime * 50f
        );
        
        // Динамические смещения с учетом сглаживания
        float lateralOffset = smoothLateralOffset;
        float verticalOffset = smoothVerticalOffset;
        
        // Целевая позиция камеры с учетом мыши
        Quaternion mouseRotation = Quaternion.Euler(mouseY, mouseX, 0f);
        Vector3 baseOffset = mouseRotation * new Vector3(
            lateralOffset, 
            desiredHeight + verticalOffset + speedFactor * 0.3f, // Уменьшен эффект высоты
            -desiredDistance
        );
        
        Vector3 targetPosition = predictedPosition + baseOffset;
        
        // Плавное движение камеры с предсказанием
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            mainCamera.transform.position,
            targetPosition,
            ref smoothPositionVelocity,
            1f / chaseSmoothSpeed,
            Mathf.Infinity,
            Time.deltaTime
        );
        
        // Точка взгляда с учетом скорости
        Vector3 lookAtPoint = predictedPosition + target.forward * smoothLookAhead;
        lookAtPoint.y += 0.3f + speedFactor * 0.5f; // Уменьшен вертикальный смещение
        
        // Добавляем небольшое боковое смещение точки взгляда в поворотах
        lookAtPoint += target.right * (targetTurnFactor * 0.3f);
        
        // Плавный поворот камеры
        Quaternion targetRotation = Quaternion.LookRotation(lookAtPoint - smoothedPosition);
        currentRotation = Quaternion.Slerp(
            mainCamera.transform.rotation,
            targetRotation,
            chaseRotationSpeed * Time.deltaTime
        );
        
        // Сохраняем текущую скорость для следующего кадра
        lastCarVelocity = carVelocity;
        
        // Применяем позицию и вращение
        mainCamera.transform.position = smoothedPosition;
        mainCamera.transform.rotation = currentRotation;
    }
    
    private void UpdateHoodCamera()
    {
        // Позиция камеры жестко привязана к машине
        Vector3 targetPosition = target.TransformPoint(hoodPosition);
        Quaternion targetRotation = target.rotation * Quaternion.Euler(hoodRotation);
        
        // Эффекты вибрации
        Vector3 shakeOffset = Vector3.zero;
        if (allowCameraShake && carRigidbody != null)
        {
            float speed = carRigidbody.linearVelocity.magnitude;
            
            // Вибрация двигателя
            float engineRPM = Mathf.Clamp01(speed / 5f);
            float engineShake = Mathf.Sin(Time.time * 30f) * 0.01f * engineRPM;
            
            // Вибрация от дороги
            float roadShake = roadBumpIntensity * hoodShakeAmount;
            
            // Общая вибрация
            shakeOffset = target.up * (engineShake + roadShake) + 
                         target.right * Mathf.Sin(Time.time * 25f) * 0.003f * engineRPM;
        }
        
        // Небольшой наклон при поворотах
        if (carRigidbody != null)
        {
            Vector3 localVelocity = target.InverseTransformDirection(carRigidbody.linearVelocity);
            float leanAngle = Mathf.Clamp(-localVelocity.x * 0.8f, -8f, 8f); // Уменьшен угол
            targetRotation *= Quaternion.Euler(0f, 0f, leanAngle);
        }
        
        mainCamera.transform.position = targetPosition + shakeOffset;
        mainCamera.transform.rotation = targetRotation;
    }
    
    private void UpdateOrbitCamera()
    {
        // Управление орбитой
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical") * (invertMouseY ? -1f : 1f);
        
        currentOrbitAngle += horizontalInput * orbitSpeed * Time.deltaTime;
        desiredHeight = Mathf.Clamp(desiredHeight + verticalInput * 0.5f, 0.5f, 10f);
        
        // Рассчитываем позицию на орбите
        float rad = currentOrbitAngle * Mathf.Deg2Rad;
        Vector3 orbitOffset = new Vector3(
            Mathf.Sin(rad) * desiredDistance,
            orbitHeight + (desiredHeight - chaseHeight),
            Mathf.Cos(rad) * desiredDistance
        );
        
        Vector3 targetPosition = target.position + orbitOffset;
        
        // Плавное движение
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            mainCamera.transform.position,
            targetPosition,
            ref currentVelocity,
            1f / orbitSmoothness
        );
        
        // Точка взгляда - центр машины
        Vector3 lookAtPoint = target.position + Vector3.up * 1f;
        Quaternion targetRotation = Quaternion.LookRotation(lookAtPoint - smoothedPosition);
        
        mainCamera.transform.position = smoothedPosition;
        mainCamera.transform.rotation = targetRotation;
    }
    
    private void HandleCollision()
    {
        if (currentMode != CameraMode.Chase) return;
        
        // Проверяем столкновения камеры с объектами
        Vector3 fromPosition = target.position + Vector3.up * desiredHeight * 0.5f;
        Vector3 toPosition = mainCamera.transform.position;
        Vector3 direction = (toPosition - fromPosition).normalized;
        float maxDistance = Vector3.Distance(fromPosition, toPosition);
        
        if (Physics.SphereCast(
            fromPosition,
            collisionRadius,
            direction,
            out collisionHit,
            maxDistance + 1f,
            collisionMask))
        {
            float safeDistance = Mathf.Max(collisionHit.distance - collisionRadius * 2f, 0.5f);
            float targetOffset = Mathf.Max(0, maxDistance - safeDistance);
            collisionOffset = Mathf.Lerp(collisionOffset, targetOffset, collisionOffsetSpeed * Time.deltaTime);
        }
        else
        {
            collisionOffset = Mathf.Lerp(collisionOffset, 0f, collisionOffsetSpeed * Time.deltaTime);
        }
        
        // Корректируем позицию камеры
        if (collisionOffset > 0.01f)
        {
            mainCamera.transform.position += mainCamera.transform.forward * collisionOffset;
        }
    }
    
    private void UpdateFieldOfView()
    {
        if (carRigidbody == null) return;
        
        // Динамическое поле зрения в зависимости от скорости
        float speed = carRigidbody.linearVelocity.magnitude;
        float targetFov = fov + Mathf.Min(speed * speedFovMultiplier, maxFov - fov);
        
        currentFov = Mathf.Lerp(currentFov, targetFov, 8f * Time.deltaTime); // Увеличена скорость смены FOV
        mainCamera.fieldOfView = currentFov;
    }
    
    private void UpdateRoadBumps()
    {
        if (carRigidbody == null) return;
        
        // Обнаружение неровностей дороги
        float verticalSpeed = Mathf.Abs((target.position.y - lastCarPosition.y) / Time.fixedDeltaTime);
        roadBumpIntensity = Mathf.Lerp(roadBumpIntensity, verticalSpeed * 0.05f, 15f * Time.fixedDeltaTime); // Уменьшен коэффициент
        lastCarPosition = target.position;
    }
    
    private void UpdateCameraEffects()
    {
        if (!allowCameraShake || carRigidbody == null) return;
        
        float speed = carRigidbody.linearVelocity.magnitude;
        
        // Интенсивность тряски зависит от скорости и неровностей
        float shakeIntensity = Mathf.Clamp01(speed / 60f) * cameraShakeAmount * 0.5f + roadBumpIntensity; // Уменьшена тряска
        
        if (shakeIntensity > 0.005f && currentMode != CameraMode.Hood)
        {
            // Более плавная тряска
            float time = Time.time * cameraShakeFrequency * 0.5f;
            
            float shakeX = Mathf.PerlinNoise(time, 0) * 2f - 1f;
            float shakeY = Mathf.PerlinNoise(0, time) * 2f - 1f;
            
            Vector3 shakeOffset = new Vector3(shakeX, shakeY, 0) * shakeIntensity * 0.1f; // Уменьшена амплитуда
            
            // Применяем тряску только в локальных координатах камеры
            mainCamera.transform.localPosition += shakeOffset;
        }
    }
    
    private void ResetCameraPosition()
    {
        if (mainCamera == null || target == null) return;
        
        switch (currentMode)
        {
            case CameraMode.Chase:
                mouseX = target.eulerAngles.y;
                mouseY = 10f;
                desiredDistance = chaseDistance;
                desiredHeight = chaseHeight;
                
                // Сброс сглаживающих переменных
                smoothPositionVelocity = Vector3.zero;
                smoothLateralOffset = 0f;
                smoothVerticalOffset = 0f;
                smoothLookAhead = chaseLookAhead;
                
                Vector3 startPos = target.position + Vector3.up * desiredHeight - target.forward * desiredDistance;
                mainCamera.transform.position = startPos;
                mainCamera.transform.LookAt(target.position + target.forward * chaseLookAhead);
                break;
                
            case CameraMode.Hood:
                mainCamera.transform.position = target.TransformPoint(hoodPosition);
                mainCamera.transform.rotation = target.rotation * Quaternion.Euler(hoodRotation);
                break;
                
            case CameraMode.Orbit:
                currentOrbitAngle = target.eulerAngles.y + 180f;
                desiredDistance = orbitDistance;
                desiredHeight = chaseHeight;
                float rad = currentOrbitAngle * Mathf.Deg2Rad;
                Vector3 orbitPos = target.position + new Vector3(
                    Mathf.Sin(rad) * desiredDistance,
                    orbitHeight,
                    Mathf.Cos(rad) * desiredDistance
                );
                mainCamera.transform.position = orbitPos;
                mainCamera.transform.LookAt(target.position + Vector3.up * 1f);
                break;
        }
    }
    
    private void ToggleCursorLock()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    // Публичные методы
    
    public void CycleCameraMode()
    {
        int modeCount = Enum.GetValues(typeof(CameraMode)).Length;
        int nextMode = ((int)currentMode + 1) % modeCount;
        currentMode = (CameraMode)nextMode;
        
        ResetCameraPosition();
        Debug.Log($"Camera mode switched to: {currentMode}");
    }
    
    public void SetCameraMode(CameraMode mode)
    {
        if (!isInitialized) return;
        
        currentMode = mode;
        ResetCameraPosition();
    }
    
    public void ToggleCameraShake()
    {
        allowCameraShake = !allowCameraShake;
        Debug.Log($"Camera shake: {(allowCameraShake ? "ON" : "OFF")}");
    }
    
    public void ResetCamera()
    {
        if (!isInitialized) return;
        
        ResetCameraPosition();
        Debug.Log("Camera reset");
    }
    
    public void SetFieldOfView(float newFov)
    {
        if (!isInitialized) return;
        
        fov = Mathf.Clamp(newFov, 40f, 100f);
        mainCamera.fieldOfView = fov;
        currentFov = fov;
    }
    
    public void SetChaseDistance(float distance)
    {
        if (!isInitialized) return;
        
        chaseDistance = Mathf.Clamp(distance, 2f, 20f);
        desiredDistance = chaseDistance;
    }
    
    public void SetChaseHeight(float height)
    {
        if (!isInitialized) return;
        
        chaseHeight = Mathf.Clamp(height, 0.5f, 10f);
        desiredHeight = chaseHeight;
    }
    
    public void SetChaseSmoothSpeed(float speed)
    {
        if (!isInitialized) return;
        
        chaseSmoothSpeed = Mathf.Clamp(speed, 1f, 20f);
    }
    
    public CameraMode GetCurrentMode()
    {
        return currentMode;
    }
    
    public Camera GetCamera()
    {
        return mainCamera;
    }
    
    public bool IsInitialized()
    {
        return isInitialized;
    }
}