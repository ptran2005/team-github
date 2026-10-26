using UnityEngine;
using UnityEngine.UI;

public class CustomerOrdering : MonoBehaviour
{
	[SerializeField] private string[] orderOptions =
	{
		"Steak",
		"Pizza",
		"Chicken",
		"Banana",
        "Salad",
        "Milkshake",
        "Burger"
	};
	[SerializeField] private Text dialogueText;

	public int ChosenNumber { get; private set; }
	public string ChosenOrder { get; private set; }
	private bool orderTaken;

	private void Start()
	{
		if (dialogueText != null)
		{
			dialogueText.gameObject.SetActive(false);
		}

		ChooseOrder();
	}

	private void OnMouseDown()
	{
		if (orderTaken || string.IsNullOrWhiteSpace(ChosenOrder))
		{
			return;
		}

		orderTaken = true;
		string message = $"I want {ChosenOrder}!";

		if (dialogueText != null)
		{
			dialogueText.text = message;
			dialogueText.gameObject.SetActive(true);
		}
		else
		{
			Debug.Log(message, this);
		}
	}

	public void ChooseOrder()
	{
		if (orderOptions == null || orderOptions.Length != 7)
		{
			Debug.LogError("CustomerOrdering needs exactly 7 order options.", this);
			return;
		}

		// Integer Random.Range uses an exclusive maximum, so this picks 1 through 7.
		ChosenNumber = Random.Range(1, 8);
		ChosenOrder = orderOptions[ChosenNumber - 1];

		if (string.IsNullOrWhiteSpace(ChosenOrder))
		{
			Debug.LogError($"No food is assigned to order number {ChosenNumber}.", this);
		}
	}
}
