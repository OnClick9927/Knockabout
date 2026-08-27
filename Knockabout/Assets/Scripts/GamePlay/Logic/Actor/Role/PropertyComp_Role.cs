using Luban;

namespace GamePlay
{
    public partial class PropertyComp_Role : PropertyComp<Luban.RoleProperty>
    {
        [Backup] public Property maxHp;
        [Backup] public Property hp;

        [Backup] public Property sight;
        [Backup] public Property armor;
        [Backup] public Property atk;
        [Backup] public Property speed;
        [Backup] public Property atkRadius;

        protected override void OnAwake()
        {
            maxHp = Create(PropertyType.MaxHP);
            hp = Create(PropertyType.HP);
            sight = Create(PropertyType.Sight);
            armor = Create(PropertyType.Armor);
            atk = Create(PropertyType.Atk);
            speed = Create(PropertyType.Speed);
            atkRadius = Create(PropertyType.AtkRadius);
        }
        protected override void OnSyncData(RoleProperty prop)
        {
            using (var scope = BeginPropChange())
            {

                scope.PushFixedProp(PropertyType.HP, prop.Hp);
                scope.PushProp(PropertyLayer.Base, PropertyType.MaxHP, prop.Hp);

                scope.PushProp(PropertyLayer.Base, PropertyType.Sight, prop.Sight);
                scope.PushProp(PropertyLayer.Base, PropertyType.Armor, prop.Armor);
                scope.PushProp(PropertyLayer.Base, PropertyType.Atk, prop.Atk);
                scope.PushProp(PropertyLayer.Base, PropertyType.Speed, prop.Speed);
                scope.PushProp(PropertyLayer.Base, PropertyType.AtkRadius, prop.AtkRaudis);
            }

        }
    }
}