using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Collections;
using UnityEngine.Networking;
using System.Text;
using UnityEditor;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using Org.BouncyCastle;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using System.Security.Cryptography;

public class SaveManager : MonoBehaviour
{
    public class PlayerData
    {
        public StatisticData statistic { get; set; } = new StatisticData();

        public AccessTokenResponse accessTokenResponse { get; set; } = new AccessTokenResponse();
        
        public OwnedCharacterInformation characterInformation { get; set; } = new OwnedCharacterInformation();
        
        public UserResponseData userData { get; set; } = new UserResponseData();

        public WalletAdressData WalletData = new WalletAdressData();

        public CharacterInfo characterInfo { get; set; } = new CharacterInfo();
    }


    public static SaveManager Instance;

    public PlayerData playerData;

    public string serverUrl;

    public string username { get; set; }

    public bool isLogin;
    public bool isWalletConnected;


    public bool isGetAchievement = false;
    public bool isSetAchievement = false;
    public string Device;
    private bool isGetStatisticCalled = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        playerData = new PlayerData();

    }

    private void SetUpDataLoad()
    {
        GetStatistic();
        CurrencyManager.Instance.Refresh();
    }



    public bool Verify(string Username, string Password, string Email, bool isLogin)
    {
        if (isLogin) {

            string jsonData = "{\"Username\": \"" + Username + "\",\"Password\": \"" + Password + "\", \"device\": \""+ Device +"\"}";
            print(jsonData);
            AccountManager.Instance.SignInP(jsonData);
            StartCoroutine(waitLoginScene());
            return true;
        }
        else
        {
            string jsonData = "{\"Password\": \"" + Password + "\",\"Username\": \"" + Username + "\",\"Email\": \"" + Email + "\"}";
            print(jsonData);
            AccountManager.Instance.SignUpP(jsonData);
            StartCoroutine(waitSignupScene());

            return true;
        }
    }

    IEnumerator waitLoginScene()
    {
        while (!AccountManager.Instance.isLogin)
        {
            yield return null;
        }
        SetUpDataLoad();
    }

    IEnumerator waitSignupScene()
    {
        while (!AccountManager.Instance.isLogin)
        {
            yield return null;
        }
    }

    public void fetchData()
    {
        GetStatistic();
        CurrencyManager.Instance.Refresh();
    }

    IEnumerator WaitForAccessToken(Action callback)
    {
        while (string.IsNullOrEmpty(playerData.accessTokenResponse.data.access_token))
        {
            yield return null;
        }

        callback?.Invoke();
    }
    public bool SetStatistic(Statistic newStatistic)
    {
        // Use a local variable to store the result
        bool requestSuccess = false;

        StartCoroutine(SetStatisticRequest(newStatistic, result =>
        {
            requestSuccess = result;
        }));

        return requestSuccess;
    }

    public IEnumerator SetStatisticRequest(Statistic newStatistic, Action<bool> resultCallback)
    {
        string url = serverUrl + "/statistics";
        string jsonData = JsonConvert.SerializeObject(newStatistic);
        string[] Data = AES_GCM_Encrypt(jsonData);
        string jsonInput = $"{{\"a\":\"{ Data.GetValue(0)}\", \"b\":\"{ Data.GetValue(1)}\", \"c\":\"{ Data.GetValue(2)}\"}}";

        UnityWebRequest request = UnityWebRequest.PostWwwForm(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonInput);
        request.certificateHandler = new CertificateWhore();
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.SetRequestHeader("Authorization", playerData.accessTokenResponse.data.access_token);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            CurrencyManager.Instance.Refresh();
            Debug.Log("Statistic data updated successfully!");
            resultCallback(true); // Indicate success
        }
        else
        {
            Debug.LogError("Failed to update statistic data: " + request.error);
            if (request.downloadHandler != null)
            {
                Debug.LogError("Response: " + request.downloadHandler.text);
            }
            Debug.Log(jsonData);
            resultCallback(false); // Indicate failure
        }
    }

    public void Withdraw(float amount)
    {
        StartCoroutine(WithdrawRequest(amount));
    }

    IEnumerator WithdrawRequest(float amount)
    {
        string url = serverUrl + "/transactions";

        // Convert data to JSON
        string jsonData = "{\"type\": \"WITHDRAW\",\"amount\":" + amount + "}";

        // Encrypt the JSON data
        string[] encryptedData = AES_GCM_Encrypt(jsonData);
        string jsonInput = $"{{\"a\":\"{encryptedData[0]}\", \"b\":\"{encryptedData[1]}\", \"c\":\"{encryptedData[2]}\"}}";

        UnityWebRequest request = UnityWebRequest.PostWwwForm(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonInput);
        request.certificateHandler = new CertificateWhore();
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.SetRequestHeader("Authorization", playerData.accessTokenResponse.data.access_token);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Withdraw Success!");
            PopUpInformationhandler.Instance.pop("Withdraw Success");
            LoadingAnimation.Instance.stopLoading();
            fetchData();
        }
        else
        {
            Debug.LogError("Failed to Withdraw: " + request.error);
            if (request.downloadHandler != null)
            {
                Debug.LogError("Response: " + request.downloadHandler.text);
                LoadingAnimation.Instance.stopLoading();
            }
            Debug.Log(jsonData);
            LoadingAnimation.Instance.stopLoading();
        }
    }


    public void Burn(float amount)
    {
        StartCoroutine(BurnRequest(amount));
    }

    IEnumerator BurnRequest(float amount)
    {
        string url = serverUrl + "/transactions";

        // Convert data to JSON
        string jsonData = "{\"type\": \"BURN\",\"amount\":" + amount + "}";

        // Encrypt the JSON data
        string[] encryptedData = AES_GCM_Encrypt(jsonData);
        string jsonInput = $"{{\"a\":\"{encryptedData[0]}\", \"b\":\"{encryptedData[1]}\", \"c\":\"{encryptedData[2]}\"}}";

        UnityWebRequest request = UnityWebRequest.PostWwwForm(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonInput);
        request.certificateHandler = new CertificateWhore();
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.SetRequestHeader("Authorization", playerData.accessTokenResponse.data.access_token);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Burn Success!");
            fetchData();
            LoadingAnimation.Instance.stopLoading();
            PopUpInformationhandler.Instance.pop("Burn Success");
        }
        else
        {
            Debug.LogError("Failed to Burn: " + request.error);
            if (request.downloadHandler != null)
            {
                Debug.LogError("Response: " + request.downloadHandler.text);
            }
            Debug.Log(jsonData);
            LoadingAnimation.Instance.stopLoading();
        }
    }

    public void GetStatistic(Action<StatisticData, bool, string> onComplete = null)
    {
        StartCoroutine(GetStatisticRequest(onComplete));
    }

    IEnumerator GetStatisticRequest(Action<StatisticData, bool, string> onComplete)
    {
        string access = playerData.accessTokenResponse.data.access_token;
        try{LoadingAnimation.Instance.toggleLoading();}catch (Exception e) { }
        string url = serverUrl + "/statistics";
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.certificateHandler = new CertificateWhore();
        request.SetRequestHeader("Authorization", access);
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonData = request.downloadHandler.text;
            StatisticData statistic = JsonConvert.DeserializeObject<StatisticData>(jsonData);
            playerData.statistic = statistic;
            CurrencyManager.Instance.Refresh();
            onComplete?.Invoke(statistic, true, null);
            try { LoadingAnimation.Instance.stopLoading(); } catch (Exception e) { }
        }
        else
        {
            string errorMessage = "Failed to fetch statistic data: " + request.error;
            Debug.LogError(errorMessage);
            PopUpInformationhandler.Instance.pop("Failed To Load Statistic Data");
            onComplete?.Invoke(null, false, errorMessage);
            try { LoadingAnimation.Instance.stopLoading(); } catch (Exception e) { }
        }
    }

    private string RandWord(int lengthWord, bool areNumber = false)
    {
        System.Random random = new System.Random();
        StringBuilder kalimat = new StringBuilder();

        string karakter;
        if (areNumber)
            karakter = "0123456789";
        else
            karakter = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()-_=+[]{}|;:,.<>?/";

        for (int i = 0; i < lengthWord; i++)
        {
            int index = random.Next(karakter.Length);
            char karakterAcak = karakter[index];
            kalimat.Append(karakterAcak);
        }

        return kalimat.ToString();
    }

    public string[] AES_GCM_Encrypt(string data)
    {
        string[] result_array = new string[3];
        // Generate encryption key (256-bit)
        byte[] key = new byte[32];
        SecureRandom random = new SecureRandom();
        random.NextBytes(key);

        // Generate nonce
        byte[] nonce = new byte[12];
        random.NextBytes(nonce);

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(data);

        // Initialize the cipher
        AeadParameters parameters = new AeadParameters(new KeyParameter(key), 128, nonce, null);
        GcmBlockCipher cipher = new GcmBlockCipher(new AesEngine());
        cipher.Init(true, parameters);

        // Allocate sufficient space for ciphertext and tag
        byte[] ciphertext = new byte[plaintextBytes.Length + 16]; // +16 for the tag
        byte[] tag = new byte[16];

        // Encrypt the data
        int len = cipher.ProcessBytes(plaintextBytes, 0, plaintextBytes.Length, ciphertext, 0);
        cipher.DoFinal(ciphertext, len);

        // Combine the ciphertext and tag
        byte[] result = new byte[ciphertext.Length];
        Array.Copy(ciphertext, 0, result, 0, ciphertext.Length);

        // Print the results
        Debug.Log("Key (Base64): " + Convert.ToBase64String(key));
        Debug.Log("Nonce: " + Convert.ToBase64String(nonce));
        Debug.Log("Ciphertext: " + Convert.ToBase64String(result));
        result_array[0] = Convert.ToBase64String(key);
        result_array[1] = Convert.ToBase64String(nonce);
        result_array[2] = Convert.ToBase64String(result);

        return result_array;
    }
}
