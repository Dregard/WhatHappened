using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class TextLink : MonoBehaviour, IPointerClickHandler
    {
        private TMP_Text _textWithLinks;


        private void Awake()
        {
            _textWithLinks = GetComponent<TMP_Text>();
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            var linkIndex = TMP_TextUtilities.FindIntersectingLink(_textWithLinks, 
                eventData.position, eventData.pressEventCamera);
            
            if (linkIndex == -1)
                return;
            
            var linkInfo = _textWithLinks.textInfo.linkInfo[linkIndex];
            var selectedLink = linkInfo.GetLinkID();
            if (selectedLink == "") 
                return;
            
            Application.OpenURL(selectedLink);
        }
    }
}