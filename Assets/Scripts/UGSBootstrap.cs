using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;

public class UGSBootstrap : MonoBehaviour
{
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Start()
    {
        await InitializeServices();   
    }

    async Task InitializeServices()
    {
        try
        {
            Debug.Log("initializing unity services");

            await UnityServices.InitializeAsync();

            Debug.Log("services installed");

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Signed in anonymously");
                Debug.Log("player id: " + AuthenticationService.Instance.PlayerId);
            }
            SceneManager.LoadScene("Menu");
        }
        catch (System.Exception e)
        {
            Debug.LogError("initialization failed:" + e.Message);
        }
    }

   
}
