using ActionBuffer;
using System;
using Lockstep;
using System.Text;
namespace GamePlay {

partial class ActorBTBlackBoard:IBackup { 
public virtual void ReadBackup(BufferReader reader){
_RuntimeValues?.Clear();
{var len = reader.ReadUInt16();
for (int i = 0; i < len; i++){
var back= reader.ReadInt32();
_RuntimeValues.Add(back);}}
;
}
public virtual void WriteBackup(BufferWriter writer){
{ writer.WriteUInt16(Convert.ToUInt16(_RuntimeValues.Count));
for (int i = 0; i < _RuntimeValues.Count; i++){
writer.WriteInt32(_RuntimeValues[i]);
}}
;
}
public virtual void DumpString(StringBuilder builder,string perfix){
builder.AppendLine($"{perfix}{nameof(_RuntimeValues)}: [");
foreach (var item in _RuntimeValues){
builder.AppendLine($"	{perfix}{item.ToString()}");
}
builder.AppendLine($"{perfix}]")
;
}
public virtual int GetHash(ref int idx){
 int hash = 1;
foreach (var item in _RuntimeValues){
hash += item.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
}
;
return hash;
}
}

partial class TransformComp:IBackup { 
public override void ReadBackup(BufferReader reader){
base.ReadBackup(reader);
position= new LVector3(LFloat.FromRaw(reader.ReadInt64()),LFloat.FromRaw(reader.ReadInt64()),LFloat.FromRaw(reader.ReadInt64()));
dir= new LVector2(LFloat.FromRaw(reader.ReadInt64()), LFloat.FromRaw(reader.ReadInt64()));
dirty= reader.ReadBool();
initPos= reader.ReadBool();
}
public override void WriteBackup(BufferWriter writer){
base.WriteBackup(writer);
writer.WriteInt64(position.x._val);
writer.WriteInt64(position.y._val);
writer.WriteInt64(position.z._val);
;
writer.WriteInt64(dir.x._val);
writer.WriteInt64(dir.y._val);;
writer.WriteBool(dirty);
writer.WriteBool(initPos);
}
public override void DumpString(StringBuilder builder,string perfix){
base.DumpString(builder,perfix);
builder.AppendLine($"{perfix}{nameof(position)}:{position.ToString()}");
builder.AppendLine($"{perfix}{nameof(dir)}:{dir.ToString()}");
builder.AppendLine($"{perfix}{nameof(dirty)}:{dirty.ToString()}");
builder.AppendLine($"{perfix}{nameof(initPos)}:{initPos.ToString()}");
}
public override int GetHash(ref int idx){
int hash = base.GetHash(ref idx);
hash += position.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += dir.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += dirty.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += initPos.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
return hash;
}
}

partial class CardComp:IBackup { 
public override void ReadBackup(BufferReader reader){
base.ReadBackup(reader);
hand?.Clear();
{var len = reader.ReadUInt16();
for (int i = 0; i < len; i++){
var back= reader.ReadInt32();
hand.Add(back);}}
;
NextGenCardTime= LFloat.FromRaw(reader.ReadInt64());
}
public override void WriteBackup(BufferWriter writer){
base.WriteBackup(writer);
{ writer.WriteUInt16(Convert.ToUInt16(hand.Count));
for (int i = 0; i < hand.Count; i++){
writer.WriteInt32(hand[i]);
}}
;
writer.WriteInt64(NextGenCardTime._val);
}
public override void DumpString(StringBuilder builder,string perfix){
base.DumpString(builder,perfix);
builder.AppendLine($"{perfix}{nameof(hand)}: [");
foreach (var item in hand){
builder.AppendLine($"	{perfix}{item.ToString()}");
}
builder.AppendLine($"{perfix}]")
;
builder.AppendLine($"{perfix}{nameof(NextGenCardTime)}:{NextGenCardTime.ToString()}");
}
public override int GetHash(ref int idx){
int hash = base.GetHash(ref idx);
foreach (var item in hand){
hash += item.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
}
;
hash += NextGenCardTime.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
return hash;
}
}

partial class InputComp:IBackup { 
public override void ReadBackup(BufferReader reader){
base.ReadBackup(reader);
}
public override void WriteBackup(BufferWriter writer){
base.WriteBackup(writer);
}
public override void DumpString(StringBuilder builder,string perfix){
base.DumpString(builder,perfix);
}
public override int GetHash(ref int idx){
int hash = base.GetHash(ref idx);
return hash;
}
}

partial class PlayerActor:IBackup { 
public override void ReadBackup(BufferReader reader){
base.ReadBackup(reader);
property.ReadBackup(reader);
input.ReadBackup(reader);
card.ReadBackup(reader);
modify.ReadBackup(reader);
buff.ReadBackup(reader);
skill.ReadBackup(reader);
ability.ReadBackup(reader);
transform.ReadBackup(reader);
}
public override void WriteBackup(BufferWriter writer){
base.WriteBackup(writer);
property.WriteBackup(writer);
input.WriteBackup(writer);
card.WriteBackup(writer);
modify.WriteBackup(writer);
buff.WriteBackup(writer);
skill.WriteBackup(writer);
ability.WriteBackup(writer);
transform.WriteBackup(writer);
}
public override void DumpString(StringBuilder builder,string perfix){
base.DumpString(builder,perfix);
builder.AppendLine($"{perfix}{nameof(property)}:");
property.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}{nameof(input)}:");
input.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}{nameof(card)}:");
card.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}{nameof(modify)}:");
modify.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}{nameof(buff)}:");
buff.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}{nameof(skill)}:");
skill.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}{nameof(ability)}:");
ability.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}{nameof(transform)}:");
transform.DumpString(builder,"\t"+perfix);
}
public override int GetHash(ref int idx){
int hash = base.GetHash(ref idx);
hash += property.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += input.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += card.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += modify.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += buff.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += skill.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += ability.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += transform.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
return hash;
}
}

partial class PropertyComp_Player:IBackup { 
public override void ReadBackup(BufferReader reader){
base.ReadBackup(reader);
maxHp.ReadBackup(reader);
hp.ReadBackup(reader);
coin.ReadBackup(reader);
}
public override void WriteBackup(BufferWriter writer){
base.WriteBackup(writer);
maxHp.WriteBackup(writer);
hp.WriteBackup(writer);
coin.WriteBackup(writer);
}
public override void DumpString(StringBuilder builder,string perfix){
base.DumpString(builder,perfix);
builder.AppendLine($"{perfix}{nameof(maxHp)}:");
maxHp.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}{nameof(hp)}:");
hp.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}{nameof(coin)}:");
coin.DumpString(builder,"\t"+perfix);
}
public override int GetHash(ref int idx){
int hash = base.GetHash(ref idx);
hash += maxHp.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += hp.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += coin.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
return hash;
}
}

partial class RoleActor:IBackup { 
public override void ReadBackup(BufferReader reader){
base.ReadBackup(reader);
buff.ReadBackup(reader);
skill.ReadBackup(reader);
ability.ReadBackup(reader);
transform.ReadBackup(reader);
move.ReadBackup(reader);
role_cfg_id= reader.ReadInt32();
role_lv= reader.ReadInt32();
bt.ReadBackup(reader);
property.ReadBackup(reader);
}
public override void WriteBackup(BufferWriter writer){
base.WriteBackup(writer);
buff.WriteBackup(writer);
skill.WriteBackup(writer);
ability.WriteBackup(writer);
transform.WriteBackup(writer);
move.WriteBackup(writer);
writer.WriteInt32(role_cfg_id);
writer.WriteInt32(role_lv);
bt.WriteBackup(writer);
property.WriteBackup(writer);
}
public override void DumpString(StringBuilder builder,string perfix){
base.DumpString(builder,perfix);
builder.AppendLine($"{perfix}{nameof(buff)}:");
buff.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}{nameof(skill)}:");
skill.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}{nameof(ability)}:");
ability.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}{nameof(transform)}:");
transform.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}{nameof(move)}:");
move.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}{nameof(role_cfg_id)}:{role_cfg_id.ToString()}");
builder.AppendLine($"{perfix}{nameof(role_lv)}:{role_lv.ToString()}");
builder.AppendLine($"{perfix}{nameof(bt)}:");
bt.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}{nameof(property)}:");
property.DumpString(builder,"\t"+perfix);
}
public override int GetHash(ref int idx){
int hash = base.GetHash(ref idx);
hash += buff.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += skill.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += ability.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += transform.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += move.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += role_cfg_id.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += role_lv.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += bt.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += property.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
return hash;
}
}

partial class RoleBTComp:IBackup { 
public override void ReadBackup(BufferReader reader){
base.ReadBackup(reader);
blackboard.ReadBackup(reader);
}
public override void WriteBackup(BufferWriter writer){
base.WriteBackup(writer);
blackboard.WriteBackup(writer);
}
public override void DumpString(StringBuilder builder,string perfix){
base.DumpString(builder,perfix);
builder.AppendLine($"{perfix}{nameof(blackboard)}:");
blackboard.DumpString(builder,"\t"+perfix);
}
public override int GetHash(ref int idx){
int hash = base.GetHash(ref idx);
hash += blackboard.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
return hash;
}
}

partial class Actor:IBackup { 
public virtual void ReadBackup(BufferReader reader){
uid= reader.ReadInt64();
type= (GamePlay.ActorType)reader.ReadEnum(typeof(GamePlay.ActorType));
playerGUID= reader.ReadUTF8();
tags.ReadBackup(reader);
for (int i = 0; i < components.Count; i++){
var back= components[i];
back.ReadBackup(reader);
};
}
public virtual void WriteBackup(BufferWriter writer){
writer.WriteInt64(uid);
writer.WriteEnum(type);
writer.WriteUTF8(playerGUID);
tags.WriteBackup(writer);
for (int i = 0; i < components.Count; i++){
var back= components[i];
back.WriteBackup(writer);
};
}
public virtual void DumpString(StringBuilder builder,string perfix){
builder.AppendLine($"{perfix}{nameof(uid)}:{uid.ToString()}");
builder.AppendLine($"{perfix}{nameof(type)}:{type.ToString()}");
builder.AppendLine($"{perfix}{nameof(playerGUID)}:{playerGUID.ToString()}");
builder.AppendLine($"{perfix}{nameof(tags)}:");
tags.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}{nameof(components)}: [");
foreach (var item in components){
builder.AppendLine($"{perfix}{{");
item.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}}}");
}
builder.AppendLine($"{perfix}]")
;
}
public virtual int GetHash(ref int idx){
 int hash = 1;
hash += uid.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += type.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += playerGUID.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += tags.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
foreach (var item in components){
hash += item.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
}
;
return hash;
}
}

partial class Component:IBackup { 
public virtual void ReadBackup(BufferReader reader){
}
public virtual void WriteBackup(BufferWriter writer){
}
public virtual void DumpString(StringBuilder builder,string perfix){
}
public virtual int GetHash(ref int idx){
 int hash = 1;
return hash;
}
}

partial class Component<T>:IBackup { 
public override void ReadBackup(BufferReader reader){
base.ReadBackup(reader);
}
public override void WriteBackup(BufferWriter writer){
base.WriteBackup(writer);
}
public override void DumpString(StringBuilder builder,string perfix){
base.DumpString(builder,perfix);
}
public override int GetHash(ref int idx){
int hash = base.GetHash(ref idx);
return hash;
}
}

partial class ActorModifyComp:IBackup { 
public override void ReadBackup(BufferReader reader){
base.ReadBackup(reader);
modifies?.Clear();
{var len = reader.ReadUInt16();
for (int i = 0; i < len; i++){
var back= reader.ReadInt32();
modifies.Add(back);}}
;
}
public override void WriteBackup(BufferWriter writer){
base.WriteBackup(writer);
{ writer.WriteUInt16(Convert.ToUInt16(modifies.Count));
for (int i = 0; i < modifies.Count; i++){
writer.WriteInt32(modifies[i]);
}}
;
}
public override void DumpString(StringBuilder builder,string perfix){
base.DumpString(builder,perfix);
builder.AppendLine($"{perfix}{nameof(modifies)}: [");
foreach (var item in modifies){
builder.AppendLine($"	{perfix}{item.ToString()}");
}
builder.AppendLine($"{perfix}]")
;
}
public override int GetHash(ref int idx){
int hash = base.GetHash(ref idx);
foreach (var item in modifies){
hash += item.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
}
;
return hash;
}
}

partial class PropertyComp:IBackup { 
public override void ReadBackup(BufferReader reader){
base.ReadBackup(reader);
}
public override void WriteBackup(BufferWriter writer){
base.WriteBackup(writer);
}
public override void DumpString(StringBuilder builder,string perfix){
base.DumpString(builder,perfix);
}
public override int GetHash(ref int idx){
int hash = base.GetHash(ref idx);
return hash;
}
}

partial class Buff:IBackup { 
public virtual void ReadBackup(BufferReader reader){
target= reader.ReadInt64();
sender= reader.ReadInt64();
layer= reader.ReadInt32();
EndTime= LFloat.FromRaw(reader.ReadInt64());
TriggerTime= LFloat.FromRaw(reader.ReadInt64());
uid= reader.ReadInt64();
cfg_id= reader.ReadInt32();
}
public virtual void WriteBackup(BufferWriter writer){
writer.WriteInt64(target);
writer.WriteInt64(sender);
writer.WriteInt32(layer);
writer.WriteInt64(EndTime._val);
writer.WriteInt64(TriggerTime._val);
writer.WriteInt64(uid);
writer.WriteInt32(cfg_id);
}
public virtual void DumpString(StringBuilder builder,string perfix){
builder.AppendLine($"{perfix}{nameof(target)}:{target.ToString()}");
builder.AppendLine($"{perfix}{nameof(sender)}:{sender.ToString()}");
builder.AppendLine($"{perfix}{nameof(layer)}:{layer.ToString()}");
builder.AppendLine($"{perfix}{nameof(EndTime)}:{EndTime.ToString()}");
builder.AppendLine($"{perfix}{nameof(TriggerTime)}:{TriggerTime.ToString()}");
builder.AppendLine($"{perfix}{nameof(uid)}:{uid.ToString()}");
builder.AppendLine($"{perfix}{nameof(cfg_id)}:{cfg_id.ToString()}");
}
public virtual int GetHash(ref int idx){
 int hash = 1;
hash += target.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += sender.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += layer.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += EndTime.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += TriggerTime.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += uid.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += cfg_id.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
return hash;
}
}

partial class BuffComp:IBackup { 
public override void ReadBackup(BufferReader reader){
base.ReadBackup(reader);
uid= reader.ReadInt64();
  GameHelper.SetListToPool(buffs);
{var len = reader.ReadUInt16();
for (int i = 0; i < len; i++){
var back= StaticPool.Get<GamePlay.Buff>();
back.ReadBackup(reader);
buffs.Add(back);}}
;
}
public override void WriteBackup(BufferWriter writer){
base.WriteBackup(writer);
writer.WriteInt64(uid);
{ writer.WriteUInt16(Convert.ToUInt16(buffs.Count));
for (int i = 0; i < buffs.Count; i++){
var back= buffs[i];
back.WriteBackup(writer);
}};
}
public override void DumpString(StringBuilder builder,string perfix){
base.DumpString(builder,perfix);
builder.AppendLine($"{perfix}{nameof(uid)}:{uid.ToString()}");
builder.AppendLine($"{perfix}{nameof(buffs)}: [");
foreach (var item in buffs){
builder.AppendLine($"{perfix}{{");
item.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}}}");
}
builder.AppendLine($"{perfix}]")
;
}
public override int GetHash(ref int idx){
int hash = base.GetHash(ref idx);
hash += uid.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
foreach (var item in buffs){
hash += item.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
}
;
return hash;
}
}

partial class SkillComp:IBackup { 

partial class Skill_CD:IBackup { 
public virtual void ReadBackup(BufferReader reader){
skill_id= reader.ReadInt32();
End= LFloat.FromRaw(reader.ReadInt64());
waitCDBegain= reader.ReadBool();
CD= LFloat.FromRaw(reader.ReadInt64());
}
public virtual void WriteBackup(BufferWriter writer){
writer.WriteInt32(skill_id);
writer.WriteInt64(End._val);
writer.WriteBool(waitCDBegain);
writer.WriteInt64(CD._val);
}
public virtual void DumpString(StringBuilder builder,string perfix){
builder.AppendLine($"{perfix}{nameof(skill_id)}:{skill_id.ToString()}");
builder.AppendLine($"{perfix}{nameof(End)}:{End.ToString()}");
builder.AppendLine($"{perfix}{nameof(waitCDBegain)}:{waitCDBegain.ToString()}");
builder.AppendLine($"{perfix}{nameof(CD)}:{CD.ToString()}");
}
public virtual int GetHash(ref int idx){
 int hash = 1;
hash += skill_id.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += End.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += waitCDBegain.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += CD.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
return hash;
}
}

partial class Modify:IBackup { 
public virtual void ReadBackup(BufferReader reader){
Skill_id= reader.ReadInt32();
indexes?.Clear();
{var len = reader.ReadUInt16();
for (int i = 0; i < len; i++){
var back= reader.ReadInt32();
indexes.Add(back);}}
;
}
public virtual void WriteBackup(BufferWriter writer){
writer.WriteInt32(Skill_id);
{ writer.WriteUInt16(Convert.ToUInt16(indexes.Count));
for (int i = 0; i < indexes.Count; i++){
writer.WriteInt32(indexes[i]);
}}
;
}
public virtual void DumpString(StringBuilder builder,string perfix){
builder.AppendLine($"{perfix}{nameof(Skill_id)}:{Skill_id.ToString()}");
builder.AppendLine($"{perfix}{nameof(indexes)}: [");
foreach (var item in indexes){
builder.AppendLine($"	{perfix}{item.ToString()}");
}
builder.AppendLine($"{perfix}]")
;
}
public virtual int GetHash(ref int idx){
 int hash = 1;
hash += Skill_id.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
foreach (var item in indexes){
hash += item.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
}
;
return hash;
}
}
public override void ReadBackup(BufferReader reader){
base.ReadBackup(reader);
  GameHelper.SetListToPool(queues);
{var len = reader.ReadUInt16();
for (int i = 0; i < len; i++){
var back= StaticPool.Get<GamePlay.SkillSignalQueue>();
back.ReadBackup(reader);
queues.Add(back);}}
;
  GameHelper.SetListToPool(cds);
{var len = reader.ReadUInt16();
for (int i = 0; i < len; i++){
var back= StaticPool.Get<GamePlay.SkillComp.Skill_CD>();
back.ReadBackup(reader);
cds.Add(back);}}
;
  GameHelper.SetListToPool(modifies);
{var len = reader.ReadUInt16();
for (int i = 0; i < len; i++){
var back= StaticPool.Get<GamePlay.SkillComp.Modify>();
back.ReadBackup(reader);
modifies.Add(back);}}
;
}
public override void WriteBackup(BufferWriter writer){
base.WriteBackup(writer);
{ writer.WriteUInt16(Convert.ToUInt16(queues.Count));
for (int i = 0; i < queues.Count; i++){
var back= queues[i];
back.WriteBackup(writer);
}};
{ writer.WriteUInt16(Convert.ToUInt16(cds.Count));
for (int i = 0; i < cds.Count; i++){
var back= cds[i];
back.WriteBackup(writer);
}};
{ writer.WriteUInt16(Convert.ToUInt16(modifies.Count));
for (int i = 0; i < modifies.Count; i++){
var back= modifies[i];
back.WriteBackup(writer);
}};
}
public override void DumpString(StringBuilder builder,string perfix){
base.DumpString(builder,perfix);
builder.AppendLine($"{perfix}{nameof(queues)}: [");
foreach (var item in queues){
builder.AppendLine($"{perfix}{{");
item.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}}}");
}
builder.AppendLine($"{perfix}]")
;
builder.AppendLine($"{perfix}{nameof(cds)}: [");
foreach (var item in cds){
builder.AppendLine($"{perfix}{{");
item.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}}}");
}
builder.AppendLine($"{perfix}]")
;
builder.AppendLine($"{perfix}{nameof(modifies)}: [");
foreach (var item in modifies){
builder.AppendLine($"{perfix}{{");
item.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}}}");
}
builder.AppendLine($"{perfix}]")
;
}
public override int GetHash(ref int idx){
int hash = base.GetHash(ref idx);
foreach (var item in queues){
hash += item.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
}
;
foreach (var item in cds){
hash += item.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
}
;
foreach (var item in modifies){
hash += item.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
}
;
return hash;
}
}

partial class PlayerData:IBackup { 

partial class RoleInfo:IBackup { 
public virtual void ReadBackup(BufferReader reader){
id= reader.ReadInt32();
level= reader.ReadInt32();
}
public virtual void WriteBackup(BufferWriter writer){
writer.WriteInt32(id);
writer.WriteInt32(level);
}
public virtual void DumpString(StringBuilder builder,string perfix){
builder.AppendLine($"{perfix}{nameof(id)}:{id.ToString()}");
builder.AppendLine($"{perfix}{nameof(level)}:{level.ToString()}");
}
public virtual int GetHash(ref int idx){
 int hash = 1;
hash += id.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += level.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
return hash;
}
}
public virtual void ReadBackup(BufferReader reader){
playerType= (GamePlay.PlayerType)reader.ReadEnum(typeof(GamePlay.PlayerType));
guid= reader.ReadUTF8();
cards?.Clear();
{var len = reader.ReadUInt16();
for (int i = 0; i < len; i++){
var back= reader.ReadInt32();
cards.Add(back);}}
;
  GameHelper.SetListToPool(roles);
{var len = reader.ReadUInt16();
for (int i = 0; i < len; i++){
var back= StaticPool.Get<GamePlay.PlayerData.RoleInfo>();
back.ReadBackup(reader);
roles.Add(back);}}
;
}
public virtual void WriteBackup(BufferWriter writer){
writer.WriteEnum(playerType);
writer.WriteUTF8(guid);
{ writer.WriteUInt16(Convert.ToUInt16(cards.Count));
for (int i = 0; i < cards.Count; i++){
writer.WriteInt32(cards[i]);
}}
;
{ writer.WriteUInt16(Convert.ToUInt16(roles.Count));
for (int i = 0; i < roles.Count; i++){
var back= roles[i];
back.WriteBackup(writer);
}};
}
public virtual void DumpString(StringBuilder builder,string perfix){
builder.AppendLine($"{perfix}{nameof(playerType)}:{playerType.ToString()}");
builder.AppendLine($"{perfix}{nameof(guid)}:{guid.ToString()}");
builder.AppendLine($"{perfix}{nameof(cards)}: [");
foreach (var item in cards){
builder.AppendLine($"	{perfix}{item.ToString()}");
}
builder.AppendLine($"{perfix}]")
;
builder.AppendLine($"{perfix}{nameof(roles)}: [");
foreach (var item in roles){
builder.AppendLine($"{perfix}{{");
item.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}}}");
}
builder.AppendLine($"{perfix}]")
;
}
public virtual int GetHash(ref int idx){
 int hash = 1;
hash += playerType.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += guid.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
foreach (var item in cards){
hash += item.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
}
;
foreach (var item in roles){
hash += item.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
}
;
return hash;
}
}

partial class SkillSignalQueue:IBackup { 

partial class DyValue:IBackup { 
public virtual void ReadBackup(BufferReader reader){
}
public virtual void WriteBackup(BufferWriter writer){
}
public virtual void DumpString(StringBuilder builder,string perfix){
}
public virtual int GetHash(ref int idx){
 int hash = 1;
return hash;
}
}
public virtual void ReadBackup(BufferReader reader){
Hited?.Clear();
{var len = reader.ReadUInt16();
for (int i = 0; i < len; i++){
var back= reader.ReadInt64();
Hited.Add(back);}}
;
sender= reader.ReadInt64();
player= reader.ReadUTF8();
type= (GamePlay.SkillEventType)reader.ReadEnum(typeof(GamePlay.SkillEventType));
startTime= LFloat.FromRaw(reader.ReadInt64());
skill_id= reader.ReadInt32();
  GameHelper.SetListToPool(dys);
{var len = reader.ReadUInt16();
for (int i = 0; i < len; i++){
var back= StaticPool.Get<GamePlay.SkillSignalQueue.DyValue>();
back.ReadBackup(reader);
dys.Add(back);}}
;
}
public virtual void WriteBackup(BufferWriter writer){
{ writer.WriteUInt16(Convert.ToUInt16(Hited.Count));
for (int i = 0; i < Hited.Count; i++){
writer.WriteInt64(Hited[i]);
}}
;
writer.WriteInt64(sender);
writer.WriteUTF8(player);
writer.WriteEnum(type);
writer.WriteInt64(startTime._val);
writer.WriteInt32(skill_id);
{ writer.WriteUInt16(Convert.ToUInt16(dys.Count));
for (int i = 0; i < dys.Count; i++){
var back= dys[i];
back.WriteBackup(writer);
}};
}
public virtual void DumpString(StringBuilder builder,string perfix){
builder.AppendLine($"{perfix}{nameof(Hited)}: [");
foreach (var item in Hited){
builder.AppendLine($"	{perfix}{item.ToString()}");
}
builder.AppendLine($"{perfix}]")
;
builder.AppendLine($"{perfix}{nameof(sender)}:{sender.ToString()}");
builder.AppendLine($"{perfix}{nameof(player)}:{player.ToString()}");
builder.AppendLine($"{perfix}{nameof(type)}:{type.ToString()}");
builder.AppendLine($"{perfix}{nameof(startTime)}:{startTime.ToString()}");
builder.AppendLine($"{perfix}{nameof(skill_id)}:{skill_id.ToString()}");
builder.AppendLine($"{perfix}{nameof(dys)}: [");
foreach (var item in dys){
builder.AppendLine($"{perfix}{{");
item.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}}}");
}
builder.AppendLine($"{perfix}]")
;
}
public virtual int GetHash(ref int idx){
 int hash = 1;
foreach (var item in Hited){
hash += item.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
}
;
hash += sender.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += player.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += type.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += startTime.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += skill_id.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
foreach (var item in dys){
hash += item.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
}
;
return hash;
}
}

partial class AbilityComp:IBackup { 

partial class Entity:IBackup { 
public virtual void ReadBackup(BufferReader reader){
id= reader.ReadInt32();
invokeTime= LFloat.FromRaw(reader.ReadInt64());
}
public virtual void WriteBackup(BufferWriter writer){
writer.WriteInt32(id);
writer.WriteInt64(invokeTime._val);
}
public virtual void DumpString(StringBuilder builder,string perfix){
builder.AppendLine($"{perfix}{nameof(id)}:{id.ToString()}");
builder.AppendLine($"{perfix}{nameof(invokeTime)}:{invokeTime.ToString()}");
}
public virtual int GetHash(ref int idx){
 int hash = 1;
hash += id.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += invokeTime.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
return hash;
}
}
public override void ReadBackup(BufferReader reader){
base.ReadBackup(reader);
  GameHelper.SetListToPool(abilities);
{var len = reader.ReadUInt16();
for (int i = 0; i < len; i++){
var back= StaticPool.Get<GamePlay.AbilityComp.Entity>();
back.ReadBackup(reader);
abilities.Add(back);}}
;
}
public override void WriteBackup(BufferWriter writer){
base.WriteBackup(writer);
{ writer.WriteUInt16(Convert.ToUInt16(abilities.Count));
for (int i = 0; i < abilities.Count; i++){
var back= abilities[i];
back.WriteBackup(writer);
}};
}
public override void DumpString(StringBuilder builder,string perfix){
base.DumpString(builder,perfix);
builder.AppendLine($"{perfix}{nameof(abilities)}: [");
foreach (var item in abilities){
builder.AppendLine($"{perfix}{{");
item.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}}}");
}
builder.AppendLine($"{perfix}]")
;
}
public override int GetHash(ref int idx){
int hash = base.GetHash(ref idx);
foreach (var item in abilities){
hash += item.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
}
;
return hash;
}
}

partial class GameData:IBackup { 
public virtual void ReadBackup(BufferReader reader){
GameType= (GamePlay.GameType)reader.ReadEnum(typeof(GamePlay.GameType));
localPlayer= reader.ReadUTF8();
randomSeed= reader.ReadInt64();
levelId= reader.ReadInt32();
  GameHelper.SetListToPool(players);
{var len = reader.ReadUInt16();
for (int i = 0; i < len; i++){
var back= StaticPool.Get<GamePlay.PlayerData>();
back.ReadBackup(reader);
players.Add(back);}}
;
}
public virtual void WriteBackup(BufferWriter writer){
writer.WriteEnum(GameType);
writer.WriteUTF8(localPlayer);
writer.WriteInt64(randomSeed);
writer.WriteInt32(levelId);
{ writer.WriteUInt16(Convert.ToUInt16(players.Count));
for (int i = 0; i < players.Count; i++){
var back= players[i];
back.WriteBackup(writer);
}};
}
public virtual void DumpString(StringBuilder builder,string perfix){
builder.AppendLine($"{perfix}{nameof(GameType)}:{GameType.ToString()}");
builder.AppendLine($"{perfix}{nameof(localPlayer)}:{localPlayer.ToString()}");
builder.AppendLine($"{perfix}{nameof(randomSeed)}:{randomSeed.ToString()}");
builder.AppendLine($"{perfix}{nameof(levelId)}:{levelId.ToString()}");
builder.AppendLine($"{perfix}{nameof(players)}: [");
foreach (var item in players){
builder.AppendLine($"{perfix}{{");
item.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}}}");
}
builder.AppendLine($"{perfix}]")
;
}
public virtual int GetHash(ref int idx){
 int hash = 1;
hash += GameType.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += localPlayer.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += randomSeed.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += levelId.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
foreach (var item in players){
hash += item.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
}
;
return hash;
}
}

partial class GameState:IBackup { 
public virtual void ReadBackup(BufferReader reader){
time= LFloat.FromRaw(reader.ReadInt64());
speed= reader.ReadInt32();
uidIndex= reader.ReadInt32();
lastFrame= reader.ReadInt64();
random= new Lockstep.Random((uint)reader.ReadUInt64());
deltaTime= LFloat.FromRaw(reader.ReadInt64());
gameStart= reader.ReadBool();
paused= reader.ReadBool();
}
public virtual void WriteBackup(BufferWriter writer){
writer.WriteInt64(time._val);
writer.WriteInt32(speed);
writer.WriteInt32(uidIndex);
writer.WriteInt64(lastFrame);
writer.WriteUInt64(random.randSeed);
writer.WriteInt64(deltaTime._val);
writer.WriteBool(gameStart);
writer.WriteBool(paused);
}
public virtual void DumpString(StringBuilder builder,string perfix){
builder.AppendLine($"{perfix}{nameof(time)}:{time.ToString()}");
builder.AppendLine($"{perfix}{nameof(speed)}:{speed.ToString()}");
builder.AppendLine($"{perfix}{nameof(uidIndex)}:{uidIndex.ToString()}");
builder.AppendLine($"{perfix}{nameof(lastFrame)}:{lastFrame.ToString()}");
builder.AppendLine($"{perfix}{nameof(random)}:{random.ToString()}");
builder.AppendLine($"{perfix}{nameof(deltaTime)}:{deltaTime.ToString()}");
builder.AppendLine($"{perfix}{nameof(gameStart)}:{gameStart.ToString()}");
builder.AppendLine($"{perfix}{nameof(paused)}:{paused.ToString()}");
}
public virtual int GetHash(ref int idx){
 int hash = 1;
hash += time.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += speed.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += uidIndex.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += lastFrame.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += random.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += deltaTime.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += gameStart.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += paused.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
return hash;
}
}

partial class PropertyComp<T>:IBackup { 
public override void ReadBackup(BufferReader reader){
base.ReadBackup(reader);
}
public override void WriteBackup(BufferWriter writer){
base.WriteBackup(writer);
}
public override void DumpString(StringBuilder builder,string perfix){
base.DumpString(builder,perfix);
}
public override int GetHash(ref int idx){
int hash = base.GetHash(ref idx);
return hash;
}
}

partial class ActorTagComp:IBackup { 
public override void ReadBackup(BufferReader reader){
base.ReadBackup(reader);
tags?.Clear();
{var len = reader.ReadUInt16();
for (int i = 0; i < len; i++){
var back= reader.ReadUTF8();
tags.Add(back);}}
;
}
public override void WriteBackup(BufferWriter writer){
base.WriteBackup(writer);
{ writer.WriteUInt16(Convert.ToUInt16(tags.Count));
for (int i = 0; i < tags.Count; i++){
writer.WriteUTF8(tags[i]);
}}
;
}
public override void DumpString(StringBuilder builder,string perfix){
base.DumpString(builder,perfix);
builder.AppendLine($"{perfix}{nameof(tags)}: [");
foreach (var item in tags){
builder.AppendLine($"	{perfix}{item.ToString()}");
}
builder.AppendLine($"{perfix}]")
;
}
public override int GetHash(ref int idx){
int hash = base.GetHash(ref idx);
foreach (var item in tags){
hash += item.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
}
;
return hash;
}
}

partial class Property:IBackup { 

partial class Layer:IBackup { 
public virtual void ReadBackup(BufferReader reader){
value= reader.ReadInt64();
percentValue= reader.ReadInt64();
layer= (GamePlay.PropertyLayer)reader.ReadEnum(typeof(GamePlay.PropertyLayer));
}
public virtual void WriteBackup(BufferWriter writer){
writer.WriteInt64(value);
writer.WriteInt64(percentValue);
writer.WriteEnum(layer);
}
public virtual void DumpString(StringBuilder builder,string perfix){
builder.AppendLine($"{perfix}{nameof(value)}:{value.ToString()}");
builder.AppendLine($"{perfix}{nameof(percentValue)}:{percentValue.ToString()}");
builder.AppendLine($"{perfix}{nameof(layer)}:{layer.ToString()}");
}
public virtual int GetHash(ref int idx){
 int hash = 1;
hash += value.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += percentValue.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
hash += layer.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
return hash;
}
}
public virtual void ReadBackup(BufferReader reader){
value= reader.ReadInt64();
  GameHelper.SetListToPool(layers);
{var len = reader.ReadUInt16();
for (int i = 0; i < len; i++){
var back= StaticPool.Get<GamePlay.Property.Layer>();
back.ReadBackup(reader);
layers.Add(back);}}
;
}
public virtual void WriteBackup(BufferWriter writer){
writer.WriteInt64(value);
{ writer.WriteUInt16(Convert.ToUInt16(layers.Count));
for (int i = 0; i < layers.Count; i++){
var back= layers[i];
back.WriteBackup(writer);
}};
}
public virtual void DumpString(StringBuilder builder,string perfix){
builder.AppendLine($"{perfix}{nameof(value)}:{value.ToString()}");
builder.AppendLine($"{perfix}{nameof(layers)}: [");
foreach (var item in layers){
builder.AppendLine($"{perfix}{{");
item.DumpString(builder,"\t"+perfix);
builder.AppendLine($"{perfix}}}");
}
builder.AppendLine($"{perfix}]")
;
}
public virtual int GetHash(ref int idx){
 int hash = 1;
hash += value.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
foreach (var item in layers){
hash += item.GetHash(ref idx) * PrimerLUT.GetPrimer(idx++);;
}
;
return hash;
}
}
}
