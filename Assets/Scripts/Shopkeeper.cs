using System.Collections;
using UnityEngine;

public enum ShopInteractionResult
{
    Entry,
    Purchased,
    CannotAfford
}

public class Shopkeeper : Interactable
{
    [SerializeField] private Transform view;
    [SerializeField] private Vector3 camRotOffset = new Vector3(42f, 180f, 0f);

    [System.Serializable]
    private class DialogueEntry
    {
        public string characterId;
        [TextArea(2, 4)] public string[] entryDialogues;
        [TextArea(2, 4)] public string[] purchaseDialogues;
        [TextArea(2, 4)] public string[] affordDialogues;

    }

    [Header("Dialogue")]
    [SerializeField] [TextArea(2, 4)] private string defaultEntryDialogue = "Take a look at my wares.";
    [SerializeField] [TextArea(2, 4)] private string defaultPurchaseDialogue = "Thank you for your purchase!";
    [SerializeField] [TextArea(2, 4)] private string defaultCannotAffordDialogue = "You don't have enough gold.";
    [SerializeField] private DialogueEntry[] dialogueByCharacter;

    private bool transitioning = false;
    private Coroutine transitionReleaseRoutine;

    public override void OnInteract(PhysicsBasedCharacterController player)
    {
        if (player == null || transitioning || CameraManager.Instance == null || CameraManager.Instance.IsTransitioning)
            return;

        transitioning = true;
        if (transitionReleaseRoutine != null)
            StopCoroutine(transitionReleaseRoutine);
        
        if (!player.inShop)
        {
            string dialogue = ResolveDialogue(player);
            CameraManager.Instance.shopCam.transform.position = view.position;
            CameraManager.Instance.shopCam.Follow = view;
            Vector3 baseEuler = transform.parent != null ? transform.parent.localEulerAngles : transform.localEulerAngles;
            CameraManager.Instance.shopCam.transform.localEulerAngles = baseEuler + camRotOffset;
            player.SetMovementEnabled(false);
            CameraManager.Instance.SwitchState(() => 
            {
                UIManager.Instance.ToggleShopUI(true);
                UIManager.Instance.SetShopDialogue(dialogue);
                player.inShop = true;
            });
        }
        else
        {
            CameraManager.Instance.SwitchState(() => 
            {
                UIManager.Instance.ToggleShopUI(false);
                player.SetMovementEnabled(true);
                player.inShop = false;
            });
        }

        transitionReleaseRoutine = StartCoroutine(ReleaseTransitionLockWhenReady());
    }

    private IEnumerator ReleaseTransitionLockWhenReady()
    {
        yield return new WaitUntil(() => CameraManager.Instance == null || !CameraManager.Instance.IsTransitioning);
        transitioning = false;
        transitionReleaseRoutine = null;
    }

    public string ResolveDialogue(PhysicsBasedCharacterController player, ShopInteractionResult interactionResult = ShopInteractionResult.Entry)
    {
        if (player == null)
            return DefaultDialogueForInteraction(interactionResult);

        string playerCharacterId = player.ShopDialogueCharacterId;

        if (!string.IsNullOrWhiteSpace(playerCharacterId) && dialogueByCharacter != null)
        {
            foreach (var entry in dialogueByCharacter)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.characterId) || entry.entryDialogues == null || entry.entryDialogues.Length == 0)
                    continue;

                if (string.Equals(entry.characterId, playerCharacterId, System.StringComparison.OrdinalIgnoreCase))
                {
                    switch (interactionResult)
                    {
                        case ShopInteractionResult.Entry:
                            return entry.entryDialogues[Random.Range(0, entry.entryDialogues.Length)];
                        case ShopInteractionResult.Purchased:
                            if (entry.purchaseDialogues != null && entry.purchaseDialogues.Length > 0)
                                return entry.purchaseDialogues[Random.Range(0, entry.purchaseDialogues.Length)];
                            break;
                        case ShopInteractionResult.CannotAfford:
                            if (entry.affordDialogues != null && entry.affordDialogues.Length > 0)
                                return entry.affordDialogues[Random.Range(0, entry.affordDialogues.Length)];
                            break;
                    }
                }
            }
        }

        return DefaultDialogueForInteraction(interactionResult);
    }

    private string DefaultDialogueForInteraction(ShopInteractionResult interactionResult)
    {
        switch (interactionResult)
        {
            case ShopInteractionResult.Entry:
                return defaultEntryDialogue;
            case ShopInteractionResult.Purchased:
                return defaultPurchaseDialogue;
            case ShopInteractionResult.CannotAfford:
                return defaultCannotAffordDialogue;
            default:
                return defaultEntryDialogue;
        }
    }
}
