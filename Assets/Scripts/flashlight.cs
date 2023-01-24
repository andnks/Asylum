using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

[RequireComponent(typeof(Light))]
[RequireComponent(typeof(AudioSource))]

public class flashlight : MonoBehaviour
{
#region Parameters
    [SerializeField] KeyCode reloadKey = KeyCode.R;
    [SerializeField] KeyCode toggleKey = KeyCode.F;

    [SerializeField] int maxBatteries = 4;
    [SerializeField] int batteries = 2;

    [SerializeField] bool autoReduce = true;
    [SerializeField] float reduceSpeed = 1.0f;

    [SerializeField] bool autoIncrease = true;
    [SerializeField] float increaseSpeed = 0.5f;

    //How long to wait until able to use flashlight again
    [Range(0, 1)]
    [SerializeField] float toggleOnWaitPercentage = 0.5f;

    public const float minBatteryLife = 0.0f;
    [SerializeField] float maxBatteryLife = 10.0f;
    [SerializeField] Quaternion offset = Quaternion.identity;
    #endregion

 #region References
    [SerializeField] AudioClip onSound = null;
    [SerializeField] AudioClip offSound = null;
    [SerializeField] AudioClip reloadSound = null;

    //UI references
    [SerializeField] Image stateIcon = null;
    [SerializeField] Slider lifeSlider = null;
    [SerializeField] Image lifeSliderFill = null;
    [SerializeField] TextMeshProUGUI reloadText = null;
    [SerializeField] TextMeshProUGUI countText = null;
    [SerializeField] CanvasGroup holder = null;

    [SerializeField] Color fullLifeColor = Color.green;
    [SerializeField] Color outLifeColor = Color.red;

    [SerializeField] new Camera camera = null;
    [SerializeField] GameObject Flashlight = null;

    #endregion

    #region Statistics

    [SerializeField]
    private float batteryLife = 0.0f;

    [SerializeField]
    private bool usingFlashlight = false;
    [SerializeField]
    private bool outOfBattery = false;

    #endregion

    #region Private and Properties

    private IEnumerator IE_UpdateBatteryLife = null;
    //light property
    Light _light = null;

    Light Light
    {
        get
        {
            if (_light == null)
            {
                _light = GetComponent<Light>();
                if (_light == null)
                {
                    _light = gameObject.AddComponent<Light>();
                }
                _light.type = LightType.Spot;
            }
            return _light;
        }

    }

    float defaultIntensity = 0.0f;

    AudioSource _source = null;
    AudioSource Source
    {
        get
        {
            if (_source == null)
            {
                _source = GetComponent<AudioSource>();
                if (_source == null) { _source = gameObject.AddComponent<AudioSource>(); }
                _source.playOnAwake = false;
            }
            return _source;
        }
    }

    float GetLifePercentage
    {
        get
        {
            return batteryLife / maxBatteryLife;
        }
    }
    

    float GetLightIntensity
    {
        get
        {
            return defaultIntensity * GetLifePercentage;
        }
    }
   
    bool CanReload
    {
        get
        {
            return usingFlashlight && (batteries > 0 && batteryLife < maxBatteryLife);
        }
    }
    
    bool MoreThanNeededPercentage
    {
        get
        {
            return GetLifePercentage >= toggleOnWaitPercentage;
        }
    }
    
    
    #endregion

    private void Start()
    {
        Init();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleFlashlight(usingFlashlight, true);
        }
        if (Input.GetKeyDown(reloadKey) && CanReload)
        {
            Reload();
        }
        if (usingFlashlight)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, camera.transform.localRotation * offset, Time.deltaTime);
            Flashlight.transform.rotation = transform.rotation;
        }
    }

    private void Reload()
    {
        batteryLife = maxBatteryLife;
        Light.intensity = GetLightIntensity;
        batteries--;

        UpdateCountText();
        UpdateSlider();

        UpdateBatteryState(false);
        UpdateBatteryLifeProcess();
        PlaySFX(reloadSound);

    }

    void ToggleFlashlight(bool state, bool playSound)
    {
        usingFlashlight = state;
        Flashlight.SetActive(state);
        state = (outOfBattery && usingFlashlight) ? false : usingFlashlight;
        ToggleObject(state);

        if (holder)
        {
            switch (usingFlashlight)
            {
                case true:
                    holder.alpha = 1.0f;
                    holder.blocksRaycasts = true;
                    break;
                case false:
                    holder.alpha = 0.0f;
                    holder.blocksRaycasts = false;
                    break;
            }
        }

        if (playSound)
        {
            PlaySFX(usingFlashlight ? onSound : offSound);

        }
        UpdateBatteryLifeProcess();
    }

    private void UpdateBatteryLifeProcess()
    {
        if (IE_UpdateBatteryLife !=null) {StopCoroutine(IE_UpdateBatteryLife); }

        if (usingFlashlight && !outOfBattery)
        {
            if (autoReduce)
            {
                IE_UpdateBatteryLife = ReduceBattery();
                StartCoroutine(IE_UpdateBatteryLife);
            }
            return;
        }
        if (autoIncrease)
        {
            IE_UpdateBatteryLife = IncreaseBattery();
            StartCoroutine(IE_UpdateBatteryLife);
        }
    }

    private IEnumerator IncreaseBattery()
    {
        while (batteryLife < maxBatteryLife)
        {
            var newValue = batteryLife + increaseSpeed * Time.deltaTime;
            batteryLife = Mathf.Clamp(newValue, minBatteryLife, maxBatteryLife);
            Light.intensity = GetLightIntensity;

            BatteryLifeCheck();

            UpdateSlider();

            yield return null;
        }
    }

    private void BatteryLifeCheck()
    {
        if (MoreThanNeededPercentage && outOfBattery)
        {
            UpdateBatteryState(false);
            UpdateBatteryLifeProcess();
        }
    }

    private IEnumerator ReduceBattery()
    {
        while (batteryLife > 0.0f)
        {
            var newValue = batteryLife - reduceSpeed * Time.deltaTime;
            batteryLife = Mathf.Clamp(newValue, minBatteryLife, maxBatteryLife);

            Light.intensity = GetLightIntensity;

            UpdateSlider();

            yield return null;
        }

        UpdateBatteryState(true);
        UpdateBatteryLifeProcess();
     
    }

    private void UpdateBatteryState(bool isDead)
    {
        outOfBattery = isDead;
        if (reloadText)
        {
            reloadText.enabled = isDead;
        }
        if (stateIcon) { stateIcon.color = isDead ? new Color(1, 1, 1, 0.5f) : new Color(255, 255, 255, 0.9f); }

        var state = outOfBattery ? false : usingFlashlight;
        ToggleObject(state);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null) { return; }
        Source.clip = clip;
       // Source.play;

    }

    void ToggleObject(bool state)
    {
        Light.enabled = state;
    }
    
    void UpdateSlider()
    {
        if (lifeSlider) { lifeSlider.value = batteryLife; }
        if (lifeSliderFill)
        {
            lifeSliderFill.color = Color.Lerp(outLifeColor, fullLifeColor, GetLifePercentage);
        }
    }

    void UpdateCountText()
    {
        if (countText)
        {
            StringBuilder countString = new StringBuilder("Batteries");
            countString.Append(batteries);
            countString.Append(" / ");
            countString.Append(maxBatteries);

            countText.text = countString.ToString();
        }

    }

    private void Init()
    {
        defaultIntensity = Light.intensity;
        batteryLife = maxBatteryLife;

        UpdateBatteryState(false);
        ToggleFlashlight(false, false);

        UpdateCountText();

        lifeSlider.minValue = minBatteryLife;
        lifeSlider.maxValue = maxBatteryLife;
        lifeSlider.value = batteryLife;
        UpdateSlider();

        if (!camera){
            camera = Camera.main;
        }

        reloadText.text = "RELOAD (" + reloadKey + ")";
    }
}
