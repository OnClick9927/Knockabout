/*********************************************************************************
*Author:         zhouchanghe
*Date:           2026-05-26
*********************************************************************************/
using IFramework;
using UnityEngine;

namespace RGBC
{
    public class HealthBarView : IFramework.GameObjectView, IPoolAbleGameObjectView
    {

        class View
        {
            //FieldsStart
            public UnityEngine.MeshRenderer HealthBar;

            //FieldsEnd

            public View(HealthBarView context)
            {
                //InitComponentsStart
                HealthBar = context.GetComponent<UnityEngine.MeshRenderer>("");

                //InitComponentsEnd
            }
        }

        private View view;

        string IPoolAbleGameObjectView.PoolKey { get; set; }

        protected override void InitComponents()
        {
            view = new View(this);
        }
        static MaterialPropertyBlock block = new MaterialPropertyBlock();
        internal void SetHp(float hp, float max)
        {
            float _Value = hp;
            float _Max = max;
            block.SetFloat(nameof(_Value), _Value);
            block.SetFloat(nameof(_Max), _Max);
            view.HealthBar.SetPropertyBlock(block);
        }
    }
}
