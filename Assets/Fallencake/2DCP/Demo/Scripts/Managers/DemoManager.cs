using UnityEngine.Rendering.Universal;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System;

public class DemoManager : MonoBehaviour
{
    #region FIELDS

    [Header("Rendering Quality:")]
    [SerializeField] UniversalRenderPipelineAsset renderPipeline;
    [SerializeField] private bool postProcessing = false;
    [SerializeField] private bool optimizedReflections = false;
    [SerializeField] private int frameRate = 60;

    [Header("Components:")]
    [SerializeField] Button postProcessingButton;
    [SerializeField] Button settingsButton;
    [SerializeField] RectTransform settingsPanel;
    public Camera waterCamera;
    [SerializeField] Camera[] waterCameras;
    [SerializeField] RenderTexture[] waterTextures;

    private UniversalAdditionalCameraData camData;
    private TextMeshProUGUI ppButtonText;
    private RectTransform settingsIcon;
    private bool isSettingsOpened = false;

    #endregion

    void Start()
    {
        Initialization();

        if (optimizedReflections)
        {
            StartCoroutine("GetReflection");
        }
    }

    private void Initialization()
    {
        ppButtonText = postProcessingButton.GetComponentInChildren<TextMeshProUGUI>();
        settingsIcon = (RectTransform)settingsButton.transform.GetChild(0);
        camData = Camera.main.GetComponent<UniversalAdditionalCameraData>();
        postProcessingButton.onClick.AddListener(SetPostProcessing);
        settingsButton.onClick.AddListener(ActivateSettingsPanel);
        SetPostProcessing(postProcessing);
        SetFrameRate(frameRate);
        SetRenderingQuality(0);
    }

    private void ActivateSettingsPanel()
    {
        isSettingsOpened = !isSettingsOpened;
        StopAllCoroutines();
        StartCoroutine(TweenPanel());
    }

    private IEnumerator TweenPanel()
    {
        float posX = isSettingsOpened ? settingsPanel.rect.width : 0;
        float iconRot = isSettingsOpened ? 180 : 0;
        float elapsedTime = 0;
        float waitTime = 0.1f;
        var currentPos = settingsPanel.anchoredPosition;
        var currentRot = settingsIcon.eulerAngles;
        var endPos = new Vector2(posX, settingsPanel.anchoredPosition.y);
        var endRot = new Vector3(settingsIcon.eulerAngles.x, settingsIcon.eulerAngles.y, iconRot);

        while (elapsedTime < waitTime)
        {
            settingsPanel.anchoredPosition = Vector2.Lerp(currentPos, endPos, (elapsedTime / waitTime));
            settingsIcon.eulerAngles = Vector3.Lerp(currentRot, endRot, (elapsedTime / waitTime));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        settingsPanel.anchoredPosition = endPos;
        settingsIcon.eulerAngles = endRot;
        yield return null;
    }

    public void SetFrameRate(int frameRate)
    {
        int targetFrameRate = frameRate > 0 ? frameRate : -1;
        Application.targetFrameRate = targetFrameRate;
    }

    private IEnumerator GetReflection()
    {
        //deactivating all additional water cameras
        for (int i = 0; i < waterCameras.Length; i++)
        {
            waterCameras[i].gameObject.SetActive(false);
        }
        yield return new WaitForSeconds(0.01f);

        //capturing images for all render textures
        for (int i = 0; i < waterTextures.Length; i++)
        {
            waterCamera.transform.position = waterCameras[i].transform.position;
            waterCamera.targetTexture = waterTextures[i];
            yield return new WaitForSeconds(0.01f);
        }
        yield return new WaitForSeconds(0.01f);

        //deactivating main water camera
        waterCamera.gameObject.SetActive(false);
    }

    public void SetRenderingQuality(int qualityValue)
    {
        float qualityFactor;
        switch (qualityValue)
        {
            case 0:
                qualityFactor = 1f;
                break;
            case 1:
                qualityFactor = 0.5f;
                break;
            case 2:
                qualityFactor = 0.25f;
                break;
            default:
                goto case 2;
        }
        SetRenderingQuality(qualityFactor);
    }

    public void SetRenderingQuality(float qualityFactor)
    {
        renderPipeline.renderScale = qualityFactor;
    }

    private void SetPostProcessing()
    {
        postProcessing = !postProcessing;
        SetPostProcessing(postProcessing);
    }

    public void SetPostProcessing(bool isEnabled)
    {
        camData.renderPostProcessing = isEnabled;
        string status = isEnabled ? "ON" : "OFF";
        ppButtonText.text = "Post Processing " + status;
    }
}
