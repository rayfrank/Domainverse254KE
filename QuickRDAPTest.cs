using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class QuickRDAPTest : MonoBehaviour
{
    IEnumerator Start()
    {
        // Replace with whatever domains you want to test
        yield return Check("google.co.ke");                 // should return REGISTERED
        yield return Check("raytechgames-co-2025.co.ke");   // likely AVAILABLE
    }

    IEnumerator Check(string fqdn)
    {
        string url = "https://rdap.kenic.or.ke/domain/" + fqdn.ToLower();
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                Debug.Log($"{fqdn}: REGISTERED (200)");
            else if ((int)req.responseCode == 404)
                Debug.Log($"{fqdn}: AVAILABLE/NOT FOUND (404)");
            else
                Debug.Log($"{fqdn}: HTTP {(int)req.responseCode}  " + req.error);
        }
    }
}
