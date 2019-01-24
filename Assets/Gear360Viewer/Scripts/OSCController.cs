using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using LitJson;
using System.IO;
using UnityEngine.UI;

// https://developers.google.com/streetview/open-spherical-camera/guides/osc/commands/execute

public class OSCController : MonoBehaviour
{

    [System.Serializable]
    private class TakePictureResult
    {
        public string fileUri; 
    }

    [System.Serializable]
    private class Progress
    {
        public double completion; 
    }

    [System.Serializable]
    private class CommandResponse
    {
        public string name;
        public string state;
        public string id;
        public Progress progress;
        public TakePictureResult results; 

    }
    [System.Serializable]
    private class ResStartSessionResult
    {
        public string sessionId;
        public int timeout;
    }

    [System.Serializable]
    private class ResStartSession
    {
        public string name;
        public string state;
        public ResStartSessionResult results;
    }

    [System.Serializable]
    private class State
    {
        public double batteryLevel;
        public string sessionId;
        public string storageUri;
        public bool storageChanged; 
        public string _captureStatus;
        public int _recordedTime;
        public int _recordableTime; 
        public string _latestFileUrl; 
        public string _batteryState;
        public int _apiVersion; 
    }

    [System.Serializable]
    private class StateFinger {
        public string fingerprint;
        public State state; 
    }

    [System.Serializable]
    private class StateFingerTime
    {
        public string stateFingerprint;
        public int throttleTimeout; 
    }


    [System.Serializable]
    private class FileFormat
    {
        public string type;
        public int width;
        public int height; 
    }


    [System.Serializable]
    private class Option
    {
        public FileFormat fileFormat; 
        public FileFormat[] fileFormatSupport; 
    }


    [System.Serializable]
    private class Result
    {
        public Option options; 
    }

    [System.Serializable]
    private class OptionRoot
    {
        public string name;
        public string state;
        public Result results; 
    }


    private const string c_HttpHead = "http://";
    private const string c_HttpPort = "80";
    [SerializeField]
    private string m_IPAddress = "192.168.107.1"; // Gear360:192.168.107.1 / Bublcam:192.168.0.100 / RICOH THETA:192.168.1.1

    void Start()
    {
        // infoデータをダウンロードしてみる
        //StartCoroutine(ExecGetInfo());
        //Capture(); 
    }

    public void Capture()
    {
        _closeButton.gameObject.SetActive(false);
        _captureButton.gameObject.SetActive(false);
        StartCoroutine(ExecCapture());
    }

    private IEnumerator ExecGetInfo()
    {
        // URLを作成
        string url = MakeAPIURL("/osc/info");

        // 送信開始
        WWW www = new WWW(url);
        yield return www;

        // 結果出力
        if (www.error == null || www.error=="")
        {
            Debug.Log(www.text);
        }
        else
        {
            Debug.Log(www.error);
        }
    }

   
    private IEnumerator ExecCommand(string command, string json)
    {
        // set header
        Dictionary<string, string> header = new Dictionary<string, string>();
        header.Add("Content-Type", "application/json;charset=utf-8");

        // set url and data
        string url = MakeAPIURL( command);
        byte[] postBytes = Encoding.Default.GetBytes(json);
        Debug.Log(url);

        // 送信開始
        WWW www = new WWW(url, postBytes, header);
        yield return www;

        // 結果出力
        if (www.error == null || www.error == "")
        {
            Debug.Log(www.text);
            _resultJson = www.text;
            _resultBytes = www.bytes; 
        }
        else
        {
            Debug.Log(www.error);
            _resultJson = null;
            _resultBytes = null; 
        }

    }



    private string MakeAPIURL(string command)
    {
        return string.Format("{0}{1}:{2}{3}", c_HttpHead, m_IPAddress, c_HttpPort, command);
    }

    private IEnumerator ExecCapture()
    {
        string json;

        // start session
        json = "{ \"name\": \"camera.startSession\", \"parameters\": { } }";
        yield return ExecCommand("/osc/commands/execute", json);

        if(_resultJson == null)
        {
            yield break; 
        }

        // set API version
        ResStartSession res_start_session = LitJson.JsonMapper.ToObject<ResStartSession>(_resultJson);

        string version = "1";
        _sessionId = res_start_session.results.sessionId;
        json = "{\"name\": \"camera.setOptions\", \"parameters\": { \"sessionId\": \"" + _sessionId + "\", \"options\": { \"clientVersion\": " + version + "} } }";
        yield return ExecCommand("/osc/commands/execute", json);

        // get pre-state
        json = "{}";
        yield return ExecCommand("/osc/state", json);

        // update property
        StateFinger res_state_finger = LitJson.JsonMapper.ToObject<StateFinger>(_resultJson);
        
        /*
        json = "{\"name\": \"camera.getOptions\", \"parameters\": { \"optionNames\": [\"fileFormat\",\"fileFormatSupport\"]}}";
        yield return ExecCommand("/osc/commands/execute", json);

        OptionRoot option = LitJson.JsonMapper.ToObject<OptionRoot>(_resultJson);

        json = "{\"name\": \"camera.setOptions\",\"parameters\": {\"options\": {\"fileFormat\": {\"type\": \"jpeg\",\"width\": 2048,\"height\": 1024}}}}";
        yield return ExecCommand("/osc/commands/execute", json);

        OptionRoot option2 = LitJson.JsonMapper.ToObject<OptionRoot>(_resultJson);

        json = "{\"name\": \"camera.getOptions\", \"parameters\": { \"optionNames\": [\"fileFormat\",\"fileFormatSupport\"]}}";
        yield return ExecCommand("/osc/commands/execute", json);

        OptionRoot option3 = LitJson.JsonMapper.ToObject<OptionRoot>(_resultJson);

    */

        // take picture
        json = "{\"name\": \"camera.takePicture\", \"parameters\": {\"sessionId\": \""+ _sessionId + "\"}}";
        yield return ExecCommand("/osc/commands/execute", json);

        CommandResponse res_takepicture = LitJson.JsonMapper.ToObject<CommandResponse>(_resultJson);

        CommandResponse res_takepicture2 = null; 

        for (int i=0;i<600;i++)
        {
            json = "{\"id\": \"" + res_takepicture.id + "\"}";
            yield return ExecCommand("/osc/commands/status", json);
            res_takepicture2 = LitJson.JsonMapper.ToObject<CommandResponse>(_resultJson);
            if(res_takepicture2.results != null)
            {
                break; 
            }

            yield return new WaitForSeconds(1.1f);
        }

        if(res_takepicture2 == null || res_takepicture2.results == null)
        {
            yield break; 
        }
        // get pre-state
        json = "{\"name\":\"camera.getImage\", \"parameters\": {\"fileUri\": \"" + res_takepicture2.results.fileUri + "\"}}";
        yield return ExecCommand("/osc/commands/execute", json);

        string pth = res_takepicture2.results.fileUri;

#if UNITY_EDITOR
        string base_path = Directory.GetCurrentDirectory();
#else
        string base_path = Application.persistentDataPath;
#endif

        string path = base_path + "/" + pth.Substring(pth.LastIndexOf('/') + 1);
        Debug.Log(path); 

        File.WriteAllBytes(path, _resultBytes);

        Texture2D texture = new Texture2D(7776, 3888);
        texture.LoadImage(_resultBytes);

        if(_sphereMaterial.mainTexture != null)
        {
            MonoBehaviour.Destroy(_sphereMaterial.mainTexture); 
        }
        _sphereMaterial.mainTexture = texture;

        _captureButton.gameObject.SetActive(true);
        _closeButton.gameObject.SetActive(true);

    }

    public void Close()
    {
        Application.Quit(); 
    }

    private string _resultJson;
    private byte[] _resultBytes; 

    private string _sessionId = null;

    [SerializeField]
    Material _sphereMaterial;

    [SerializeField]
    Button _captureButton;

    [SerializeField]
    Button _closeButton;
}