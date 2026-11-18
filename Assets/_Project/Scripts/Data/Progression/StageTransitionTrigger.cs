using UnityEngine;
public class StageTransitionTrigger : MonoBehaviour
{
    [Header("Destino")]
    [SerializeField] private string _nextSceneName = "Stage2_Catedral";
    [SerializeField] private int _stateOnEnterNextStage = 4;
    [SerializeField] private int _requiredStateToCross = 3;

    private bool _isTransitioning;
    private async void OnTriggerEnter(Collider other)
    {
        if (_isTransitioning || !other.CompareTag("Player")) return;

        if (StateManager.Instance != null && StateManager.Instance.CurrentState < _requiredStateToCross)
            return;

        _isTransitioning = true;

        if (SaveManager.Instance != null)
        {
            await SaveManager.Instance.TransitionToNextStageAsync(_nextSceneName, _stateOnEnterNextStage);
        }
    }
}