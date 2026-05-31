using System;
using System.IO;
using UnityEngine;

[Serializable]
public class SettingData
{
    // 오디오
    public float masterVolume = 0.8f;
    public float sfxVolume = 0.8f;
    public float bgmVolume = 0.8f;

    // 그래픽
    public int resolutionWidth = 1920;
    public int resolutionHeight = 1080;
    public bool isFullscreen = true;
    public int fpsLimit = 60;

    // 키 바인딩 (Input System의 Overrides JSON 문자열을 그대로 저장)
    public string keyBindingsJson = "";

    // 언어
    public string languageCode = "KO";
}