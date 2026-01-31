using UnityEngine;

public class CarCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 2.5f, -6f);
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float lookAheadDistance = 5f;
    
    [Header("Advanced")]
    [SerializeField] private bool smoothFollow = true;
    [SerializeField] private float minFollowSpeed = 5f;
    [SerializeField] private float maxFollowSpeed = 15f;
    
    private Camera mainCamera;
    private Transform carTransform;
    private Vector3 currentVelocity;
    
    private void Start()
    {
        carTransform = transform;
        
        // Создаем или находим главную камеру
        mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            GameObject cameraObj = new GameObject("MainCamera");
            mainCamera = cameraObj.AddComponent<Camera>();
            cameraObj.tag = "MainCamera";
            
            // Настройки камеры для лучшего качества
            mainCamera.fieldOfView = 60f;
            mainCamera.nearClipPlane = 0.3f;
            mainCamera.farClipPlane = 1000f;
            
            Debug.Log("✓ Main Camera created");
        }
        
        // Отключаем аудио слушатель на машине, если он есть
        var listener = GetComponent<AudioListener>();
        if (listener != null)
        {
            Destroy(listener);
        }
        
        // Добавляем аудио слушатель на камеру
        if (mainCamera.GetComponent<AudioListener>() == null)
        {
            mainCamera.gameObject.AddComponent<AudioListener>();
        }
        
        Debug.Log("✓ Camera controller initialized");
    }
    
    private void LateUpdate()
    {
        if (mainCamera == null || carTransform == null) return;
        
        // Вычисляем целевую позицию камеры
        Vector3 targetPosition = carTransform.position + carTransform.TransformDirection(cameraOffset);
        
        // Плавное следование
        if (smoothFollow)
        {
            // Динамическая скорость в зависимости от расстояния
            float distance = Vector3.Distance(mainCamera.transform.position, targetPosition);
            float dynamicSpeed = Mathf.Lerp(minFollowSpeed, maxFollowSpeed, distance / 10f);
            
            mainCamera.transform.position = Vector3.SmoothDamp(
                mainCamera.transform.position,
                targetPosition,
                ref currentVelocity,
                1f / dynamicSpeed
            );
        }
        else
        {
            mainCamera.transform.position = Vector3.Lerp(
                mainCamera.transform.position,
                targetPosition,
                followSpeed * Time.deltaTime
            );
        }
        
        // Камера смотрит немного впереди машины
        Vector3 lookTarget = carTransform.position + carTransform.forward * lookAheadDistance;
        
        // Плавный поворот камеры
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - mainCamera.transform.position);
        mainCamera.transform.rotation = Quaternion.Slerp(
            mainCamera.transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
    
    private void OnDestroy()
    {
        // При уничтожении контроллера не удаляем камеру полностью
        // она может использоваться другими системами
        Debug.Log("Camera controller destroyed");
    }
    
    // Публичные методы для настройки камеры во время игры
    public void SetCameraDistance(float distance)
    {
        cameraOffset = new Vector3(cameraOffset.x, cameraOffset.y, -distance);
    }
    
    public void SetCameraHeight(float height)
    {
        cameraOffset = new Vector3(cameraOffset.x, height, cameraOffset.z);
    }
    
    public void SetFollowSpeed(float speed)
    {
        followSpeed = speed;
    }
}