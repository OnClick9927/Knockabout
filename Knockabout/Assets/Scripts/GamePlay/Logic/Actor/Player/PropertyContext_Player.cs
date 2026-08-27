
using Luban;

namespace GamePlay
{

    [Backup]
    public partial class PropertyComp_Player : PropertyComp<PlayerProperty>
    {
        [Backup] public Property maxHp;
        [Backup] public Property hp;
        [Backup] public Property coin;
        protected override void OnAwake()
        {
            maxHp = Create(PropertyType.MaxHP);
            hp = Create(PropertyType.HP);
            coin = Create(PropertyType.Coin);
        }

        protected override void OnSyncData(PlayerProperty data)
        {
            using (var scope = BeginPropChange())
            {
                scope.PushFixedProp(PropertyType.Coin, data.Coin);
                scope.PushFixedProp(PropertyType.HP, data.HP);
                scope.PushProp(PropertyLayer.Base, PropertyType.MaxHP, data.HP);
            }
        }
    }
}
