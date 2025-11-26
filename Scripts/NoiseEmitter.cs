using UnityEngine;

/// <summary>
/// Zombilerin duyabileceği sesleri yayar. Player'a ekle.
/// Ses çıkarıldığında geçici trigger sphere'ler oluşturur.
/// Farklı aksiyonlar farklı ses yoğunluğu yaratır (yürü, koş, zıpla).
/// Observer Pattern kullanıyor - event-driven mimari için.
/// </summary>
public class NoiseEmitter : MonoBehaviour
{
    // Event sistemi: Zombiler trigger check yerine buna subscribe oluyor
    // Her frame kontrol etmek yerine event dinliyorlar - performans için
    public static event System.Action<Vector3, float> OnNoiseCreated;


    [Header("Noise Intensities (Hearing Radius)")]
    [Tooltip("How far crouching/sneaking can be heard (units)")]
    public float crouchNoiseRadius = 3f;
    [Tooltip("How far walking can be heard (units)")]
    public float walkNoiseRadius = 8f;
    [Tooltip("How far running can be heard (units)")]
    public float runNoiseRadius = 15f;
    [Tooltip("How far jumping can be heard (units)")]
    public float jumpNoiseRadius = 12f;
    [Tooltip("How far landing can be heard (units)")]
    public float landNoiseRadius = 10f;

    [Header("Noise Duration")]
    [Tooltip("How long footstep noise lingers (seconds)")]
    public float footstepDuration = 0.5f;
    [Tooltip("How long action noise (jump, gunshot) lingers (seconds)")]
    public float actionNoiseDuration = 1.5f;

    [Header("Footstep Settings")]
    [Tooltip("Time between footstep noise emissions while moving (seconds)")]
    public float footstepInterval = 0.4f;
    [Tooltip("Minimum movement speed to make footstep noise")]
    public float movementThreshold = 0.1f;

    [Header("Landing Settings")]
    [Tooltip("Minimum time in air before landing noise triggers (prevents ground flicker)")]
    public float minAirborneTime = 0.2f;
    [Tooltip("Minimum fall distance to make landing noise (meters)")]
    public float minFallDistance = 0.3f;
    [Tooltip("Fall distance for maximum landing noise (meters)")]
    public float maxFallDistance = 5f;
    [Tooltip("Maximum landing noise radius (at maxFallDistance or higher)")]
    public float maxLandNoiseRadius = 20f;

    [Header("References")]
    [Tooltip("PlayerMovement component - will auto-find if empty")]
    public PlayerMovement playerMovement;

    [Header("Debug")]
    [Tooltip("Show noise spheres in Scene view")]
    public bool showDebugGizmos = true;

    // İç değişkenler - ayak sesi tracking için
    private float footstepTimer = 0f; // son ayak sesinden beri geçen süre
    private bool wasGroundedLastFrame = false; // iniş algılama için (false başlat ki ilk frame'de sahte iniş olmasın)
    private Vector3 lastPosition; // hareket hızı hesaplamak için
    private float airborneTime = 0f; // havada kalma süresi
    private float takeoffHeight = 0f; // yerden ayrılırken Y pozisyonu
    private float maxHeightReached = 0f; // düşüş mesafesi için en yüksek nokta

    void Awake()
    {
        // PlayerMovement'ı otomatik bul
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                Debug.LogError("NoiseEmitter: PlayerMovement bulunamadı! Bunu Player objesine ekle.");
            }
        }

        lastPosition = transform.position;
        takeoffHeight = transform.position.y;
        maxHeightReached = transform.position.y;
    }

    void Update()
    {
        // Hareket hızını kontrol et
        float movementSpeed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;

        bool isMoving = movementSpeed > movementThreshold;
        bool isGrounded = playerMovement != null && playerMovement.IsGrounded();

        // Havada kalma süresini takip et (landing detection'dan ÖNCE yap)
        if (!isGrounded)
        {            // Havadayız - timer'ı artır
            airborneTime += Time.deltaTime;

            // Uçuş sırasında ulaşılan maksimum yüksekliği kaydet
            if (transform.position.y > maxHeightReached)
            {
                maxHeightReached = transform.position.y;
            }
        }        // Ayak sesi: yerde hareket ederken periyodik olarak çıkar
        if (isMoving && isGrounded)
        {
            footstepTimer += Time.deltaTime;

            // Ayak sesi zamanı geldi mi?
            if (footstepTimer >= footstepInterval)
            {
                footstepTimer = 0f; // timer'ı sıfırla
                EmitFootstepNoise();
            }
        }
        else
        {
            // Duruyor: timer'ı sıfırla ki sonraki adım hemen olsun
            footstepTimer = footstepInterval;
        }

        // Yerden ayrılma anını algıla - yüksekliği kaydet
        if (!isGrounded && wasGroundedLastFrame)
        {
            takeoffHeight = transform.position.y;
            maxHeightReached = transform.position.y; // max yükseklik tracker'ını sıfırla
        }

        // İniş sesi: zıplama/düşmeden sonra yere inişi algıla
        if (isGrounded && !wasGroundedLastFrame)
        {
            // Ulaşılan en yüksek noktadan düşüş mesafesini hesapla
            float fallDistance = maxHeightReached - transform.position.y;

            // İniş sesini SADECE şu durumlarda çıkar:
            // 1. Yeterince havada kaldıysa (ground flicker'ı önlemek için)
            // 2. Yeterince düştüyse (küçük tümsekleri önlemek için)
            if (airborneTime >= minAirborneTime && fallDistance >= minFallDistance)
            {
                EmitLandingNoise(fallDistance);
            }

            // Airborne timer'ını landing detection'dan SONRA sıfırla
            airborneTime = 0f;
        }

        wasGroundedLastFrame = isGrounded;
    }

    /// <summary>
    /// Hareket durumuna göre ayak sesi çıkar (çömel, yürü, koş).
    /// Hareket ederken periyodik olarak çağrılıyor.
    /// </summary>
    void EmitFootstepNoise()
    {
        if (playerMovement == null) return;

        float noiseRadius = 0f;

        // Hareket durumuna göre ses yarıçapını belirle
        if (playerMovement.IsCrouched)
        {
            // Çömelerek: çok sessiz
            noiseRadius = crouchNoiseRadius;
        }
        else
        {
            // Koşuyor mu kontrol et (bunu PlayerMovement'tan expose etmem lazım)
            // Şimdilik yürüme hızını kullan
            // TODO: koşma algılama ekle
            noiseRadius = walkNoiseRadius;
        }

        // Sesi oluştur
        CreateNoise(transform.position, noiseRadius, footstepDuration, NoiseType.Footstep);
    }

    /// <summary>
    /// Yere inişte ses çıkar.
    /// Ses yarıçapı düşüş mesafesine göre scale olur (yüksekten düştü = daha gürültülü).
    /// </summary>
    /// <param name="fallDistance">Kaç metre düştü</param>
    void EmitLandingNoise(float fallDistance)
    {
        // Düşüş mesafesine göre ses yarıçapını scale et
        float t = Mathf.InverseLerp(minFallDistance, maxFallDistance, fallDistance);
        float scaledRadius = Mathf.Lerp(landNoiseRadius, maxLandNoiseRadius, t);

        CreateNoise(transform.position, scaledRadius, actionNoiseDuration, NoiseType.Landing);
    }

    /// <summary>
    /// Zıplama sesi çıkar. PlayerMovement'tan zıplarken bunu çağır.
    /// </summary>
    public void EmitJumpNoise()
    {
        CreateNoise(transform.position, jumpNoiseRadius, actionNoiseDuration, NoiseType.Jump);
    }

    /// <summary>
    /// Özel ses çıkar (silah sesi, kapı çarpması vs için).
    /// </summary>
    public void EmitNoise(Vector3 position, float radius, float duration, NoiseType type = NoiseType.Other)
    {
        CreateNoise(position, radius, duration, type);
    }

    /// <summary>
    /// Belirtilen yerde ses sphere'i oluştur - object pooling kullanarak.
    /// Instantiate/Destroy yerine pool'dan al - zero GC allocation.
    /// Ayrıca event de fire ediliyor (Observer Pattern).
    /// </summary>
    void CreateNoise(Vector3 position, float radius, float duration, NoiseType type)
    {
        if (radius <= 0f) return; // radius 0 ise ses yok

        // Pool'dan al (sıfır allocation, GC yok)
        GameObject noiseObject = NoisePool.Instance.GetNoise(position, radius, duration, type, this.gameObject);

        // Event fire et: bütün subscriber'lara (zombilere) ses oluştuğunu bildir
        // Observer Pattern: ses oluşturmayı detection'dan ayırıyor
        OnNoiseCreated?.Invoke(position, radius);

        // Pool otomatik olarak süre bitince objeyi geri alıyor - manuel Destroy yok!
    }

    // Gizmos: Scene view'da sesi görselleştir
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Player'ın potansiyel ses yarıçapını çiz (duruma göre)
        if (playerMovement != null && playerMovement.IsCrouched)
        {
            Gizmos.color = new Color(0, 1, 0, 0.2f); // Yeşil = sessiz
            Gizmos.DrawSphere(transform.position, crouchNoiseRadius);
        }
        else
        {
            Gizmos.color = new Color(1, 1, 0, 0.2f); // Sarı = normal
            Gizmos.DrawSphere(transform.position, walkNoiseRadius);
        }
    }
}

/// <summary>
/// Farklı davranışlar için ses tipleri.
/// Zombiler ayak sesine karşı silah sesinden farklı tepki verebilir.
/// </summary>
public enum NoiseType
{
    Footstep,   // Yürüme/koşma
    Jump,       // Zıplama
    Landing,    // İniş
    Gunshot,    // Silah sesi
    DoorSlam,   // Kapı çarpması
    Other       // Genel ses
}

/// <summary>
/// Ses sphere GameObject'lerine eklenen component.
/// Zombilerin okuyabileceği ses datasını tutuyor.
/// </summary>
public class NoiseSource : MonoBehaviour
{
    public NoiseType noiseType;
    public float noiseRadius;
    public Vector3 noisePosition;
    public GameObject emitter; // Sesi kim çıkardı (genelde player)

    // Gizmos: aktif ses sphere'lerini Scene view'da göster
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f); // Kırmızı = aktif ses
        Gizmos.DrawWireSphere(transform.position, noiseRadius);
    }
}
