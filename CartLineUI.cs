using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CartLineUI : MonoBehaviour
{
    public TMP_Text domainText;
    public Button removeButton;

    string domain;
    CartSystemAuto cart;

    public void Bind(string domain, CartSystemAuto cart)
    {
        this.domain = domain;
        this.cart = cart;

        if (!domainText)  domainText  = transform.Find("DomainText")?.GetComponent<TMP_Text>()
                               ?? GetComponentInChildren<TMP_Text>(true);
        if (!removeButton) removeButton = transform.Find("RemoveButton")?.GetComponent<Button>()
                               ?? GetComponentInChildren<Button>(true);

        if (domainText) domainText.text = domain;
        if (removeButton) removeButton.onClick.AddListener(() => cart.Remove(this.domain));
    }
}
