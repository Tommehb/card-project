using UnityEngine;

public class Door : MonoBehaviour
{
    public GameObject interactionPrompt;
    public GameObject doorModel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && interactionPrompt.activeSelf)
        {
            ToggleDoor();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            interactionPrompt.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            interactionPrompt.SetActive(false);
        }
    }

    public void ToggleDoor()
    {
        if (doorModel != null)
        {
            doorModel.SetActive(!doorModel.activeSelf);
        }
    }
}
