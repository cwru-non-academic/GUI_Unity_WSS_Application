using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.MUIP;
using TMPro;
using System.ComponentModel;
using System.Xml.Linq;
using UnityEngine.UI;
using UnityEditor.PackageManager;

public class WaveMaker : MonoBehaviour
{
    private const int pointCount = 32;
    private const int maxAmp = 255;
    private const int rateMultiplier = 5;
    private const float circleSize = 10f;

    [SerializeField] private CustomDropdown points;
    [SerializeField] private TMP_Text valueTxt;
    [SerializeField] private TMP_Text rateTxt;
    [SerializeField] private RectTransform container;
    [SerializeField] private Stimulation stim;
    [SerializeField] private CustomInputField fileInput;

    private int currentIndex =0;
    private float rate = 1.0f;
    private int[] wave;
    private GameObject[] UIdots;
    private float horConv = 0;
    private float verConv = 0;
    private float containerX = 0;
    private float containerY = 0;

    // Start is called before the first frame update
    void Start()
    {
        addPoints();

        //init array with hight
        wave = new int[pointCount];
        UIdots = new GameObject[pointCount];
        Canvas.ForceUpdateCanvases();
        containerX = Mathf.Abs(container.rect.width);
        containerY = Mathf.Abs(container.rect.height);
        horConv = containerX / (pointCount+1);
        verConv = containerY / 100.0f;
        for (int i = 0; i < pointCount; i++)
        {
            wave[i] = 0;
            UIdots[i] = CreatePoint(i+1, 0);
        }
        refreshRate();
        refreshValue();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            changePointAmp(1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            changePointAmp(-1);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            updatePoints(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            updatePoints(1);
        }else if (Input.mouseScrollDelta.y > 0)
        {
            changePointAmp(1);
        }
        else if (Input.mouseScrollDelta.y < 0)
        {
            changePointAmp(-1);
        }
    }

    private void addPoints()
    {
        points.items.Clear();
        for(int i = 0; i < pointCount; i++)
        {
            points.CreateNewItem(i.ToString(), false);
        }
        points.SetupDropdown();
        points.SetDropdownIndex(currentIndex);
    }

    public void changePoint()
    {
        currentIndex = points.index;
        refreshValue();
    }

    public void updatePoints(int change)
    {
        points.SetDropdownIndex(currentIndex+=change);
        refreshValue();
    }

    public void changePointAmp(int polarity)
    {
        if(polarity >0 && wave[currentIndex]<maxAmp)
        {
            wave[currentIndex] += (int)(polarity * rate * 0.01 * maxAmp);
            if (wave[currentIndex] > maxAmp)
            {
                wave[currentIndex] = maxAmp;
            }
        } else if(polarity < 0 && wave[currentIndex] >0)
        {
            wave[currentIndex] += (int)(polarity * rate * 0.01 * maxAmp);
            if (wave[currentIndex] < 0)
            {
                wave[currentIndex] = 0;
            }
        }
        updateDot(UIdots[currentIndex], 100* wave[currentIndex] / maxAmp);
        refreshValue();
    }

    private void refreshValue()
    {
        valueTxt.text = (100*wave[currentIndex]/maxAmp).ToString("F2")+"%";
    }

    private void refreshRate()
    {
        rateTxt.text = rate.ToString("F2") + "%";
    }

    public void changeRate(int polarity)
    {
        if(polarity > 0 && rate<20)
        {
            rate *= rateMultiplier;
        } else if (polarity<0 && rate>0.1) 
        { 
            rate /= rateMultiplier;
        }
        refreshRate();
    }

    private GameObject CreatePoint(float x, float y)
    {
        GameObject circle = new GameObject("circle", typeof(RawImage));
        circle.transform.SetParent(container, false);
        circle.GetComponent<RectTransform>().anchoredPosition = new Vector2((x * horConv) - (containerX / 2.0f), (y * verConv) - (containerY / 2.0f));
        circle.GetComponent<RectTransform>().sizeDelta = new Vector2(circleSize, circleSize);
        return circle;
    }

    private void updateDot(GameObject dot, float amplitude)
    {
        RectTransform dotT= dot.GetComponent<RectTransform>();
        dotT.anchoredPosition = new Vector2(dotT.anchoredPosition.x, (amplitude * verConv) - (containerY / 2.0f));
    }

    public void load()
    {
        try
        {
            Waveform shape = JsonUtility.FromJson<Waveform>(System.IO.File.ReadAllText(Application.streamingAssetsPath + "/" + fileInput.inputText.text + "WF.json"));
            wave = shape.catShape;
            updateAlLUIDots();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("JSON loading error: " + ex.Message);
        }
    }

    public void upload()
    {
        WaveformBuilder w = new WaveformBuilder(wave);
        stim.updateWaveform(w, 1);
    }

    public void save()
    {
        WaveformBuilder w = new WaveformBuilder(wave);
        string toJsSON = JsonUtility.ToJson(w.getWave());
        System.IO.File.WriteAllText(Application.streamingAssetsPath + "/" + fileInput.inputText.text + "WF.json", toJsSON);
    }

    private void updateAlLUIDots()
    {
        for (int i = 0; i < pointCount; i++)
        {
            updateDot(UIdots[i], 100 * wave[i] / maxAmp);
        }
        changePoint();
    }
}
