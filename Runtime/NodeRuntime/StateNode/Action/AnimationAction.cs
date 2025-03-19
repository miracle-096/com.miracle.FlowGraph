using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace FlowGraph.Node
{
    public class AnimationAction : BaseAction
    {
        [Header("AnimatorAction")]
        public Animator animator;
        public bool waitUntilFinish = true;
        [ValueDropdown(nameof(Animations))] public AnimationClip animationClip;

        [ValueDropdown(nameof(Animations)), ShowIf("ignore")] public AnimationClip ignoreActionInState;

        public float timerDelta = 0f;

        public override async UniTask RunningLogicAsync()
        {
            animator.Play(animationClip.name);

            if (!waitUntilFinish)
            {
                await RunOverAsync();
            }
            else
            {
                if (animationClip.isLooping)
                {
                    await RunOverAsync();
                }
                else
                {
                    await UniTask.Delay((int)((animationClip.length + timerDelta) * 1000));
                    await RunOverAsync();
                }
            }
        }

        List<AnimationClip> Animations()
        {
            if (animator != null)
            {
                return animator.runtimeAnimatorController.animationClips.ToList();
            }

            return null;
        }
    }
}