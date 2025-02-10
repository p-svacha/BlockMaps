using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TheThoriumChallenge
{
    public class UI_ActionTimeline : MonoBehaviour
    {
        public GameObject Container;
        public UI_ActionTimelineElement ElementPrefab;

        public void Refresh(PriorityQueue<Creature> ActionQueue)
        {
            HelperFunctions.DestroyAllChildredImmediately(Container);

            foreach(Creature entity in ActionQueue.ToSortedList())
            {
                if(entity.IsVisibleBy(Game.Instance.CurrentLevel.LocalPlayer))
                {
                    UI_ActionTimelineElement elem = Instantiate(ElementPrefab, Container.transform);
                    elem.Init(entity);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }
}
