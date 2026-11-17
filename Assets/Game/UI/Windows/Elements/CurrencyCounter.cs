using Game.Currency;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.UI.Windows.Elements
{
    public class CurrencyCounter : MonoBehaviour
    {
        [SerializeField] private TMP_Text _amountTmp;
        [SerializeField] private Image _currencyImage;
        [SerializeField] private CurrencyKind _currencyKind;
        [SerializeField] private bool _isAutoUpdate = false;

        private CurrencySystem _currencySystem;

        private int _newAmount;
        private int _pendingDisplayAmount;


        /// <summary>Subscribes after injection and reads the balance, including rewards granted before creation.</summary>
        private void Start()
        {
            _currencySystem.CurrencyChangedEvent += HandleCurrencyChanged;
            
            RefreshBalance();
        }

        /// <summary>Refreshes reused counters when their HUD or window becomes visible again.</summary>
        private void OnEnable()
        {
            if (_currencySystem != null)
                RefreshBalance();
        }

        /// <summary>Releases the currency subscription with the counter's lifetime.</summary>
        private void OnDestroy()
        {
            if (_currencySystem != null)
                _currencySystem.CurrencyChangedEvent -= HandleCurrencyChanged;
        }

        public Vector3 IconPosition => _currencyImage.rectTransform.position;
        
        /// <summary>Receives the shared currency API before the counter starts observing balances.</summary>
        [Inject]
        public void Construct(CurrencySystem currencySystem)
        {
            _currencySystem = currencySystem;
        }
        
        /// <summary>Shows the latest observed balance after a manually controlled reward animation.</summary>
        public void UpdateAmount()
        {
            SetAmount(_newAmount - _pendingDisplayAmount);
        }

        /// <summary>Hides an incoming increase until its presentation arrives; call before granting the currency.</summary>
        public void DeferIncrease(int amount)
        {
            if (amount < 0)
                throw new System.ArgumentOutOfRangeException(nameof(amount));
            _pendingDisplayAmount += amount;
        }

        /// <summary>Reveals only the arriving increase, retaining later queued rewards as hidden presentation.</summary>
        public void RevealIncrease(int amount)
        {
            if (amount < 0 || amount > _pendingDisplayAmount)
                throw new System.ArgumentOutOfRangeException(nameof(amount));
            _pendingDisplayAmount -= amount;
            RefreshBalance();
        }

        /// <summary>Reveals the authoritative balance immediately when its deferred presentation is cancelled; never grants currency.</summary>
        public void RevealAllIncreases()
        {
            _pendingDisplayAmount = 0;
            RefreshBalance();
        }

        /// <summary>Reads both the displayed and deferred balance from the authoritative currency state.</summary>
        private void RefreshBalance()
        {
            _newAmount = _currencySystem.GetCurrencyValue(_currencyKind);
            UpdateAmount();
        }

        /// <summary>Uses the existing integer number format for every currency.</summary>
        private void SetAmount(int amount)
        {
            _amountTmp.text = amount.ToString();
        }

        /// <summary>Tracks only this currency, preserving optional animation-controlled display updates.</summary>
        private void HandleCurrencyChanged(CurrencyKind kind, int amount)
        {
            if (_currencyKind != kind)
                return;

            _newAmount = amount;
            if (_isAutoUpdate)
                UpdateAmount();
        }
    }
}
