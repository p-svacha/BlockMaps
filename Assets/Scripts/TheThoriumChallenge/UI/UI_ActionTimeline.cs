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

            List<Creature> fullTimeline = ActionQueue.ToSortedList();

            foreach (Creature creature in fullTimeline)
            {
                if(creature.IsVisibleBy(Game.Instance.CurrentStage.LocalPlayer))
                {
                    UI_ActionTimelineElement elem = Instantiate(ElementPrefab, Container.transform);
                    bool isActingNow = (creature == fullTimeline[0]);
                    elem.Init(creature, isActingNow);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }
}
