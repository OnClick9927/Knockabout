namespace WooLocalization{
using System.Collections.Generic;
public class LocalizationKeys {
public class String {
public const string EightDot="EightDot";
public const string Login_Btn="Login/Btn";
public const string Test="Test";
public const string Att_ShelfCategoryQuantity="Att_ShelfCategoryQuantity";
public const string Att_ShelfQuantity="Att_ShelfQuantity";
public const string Att_CustomerEntryInterval="Att_CustomerEntryInterval";
public const string Att_CustomerEntryIntervalBonus="Att_CustomerEntryIntervalBonus";
public const string Att_ShopCustomerCapacity="Att_ShopCustomerCapacity";
public const string Att_ShopCustomerCapacityBonus="Att_ShopCustomerCapacityBonus";
public const string Att_ThemePrice="Att_ThemePrice";
public const string Att_ThemePriceBonus="Att_ThemePriceBonus";
public const string Att_GoodsPriceBonus="Att_GoodsPriceBonus";
public const string Att_GoodsPrice="Att_GoodsPrice";
public const string Att_MangHeGoldDropLimit="Att_MangHeGoldDropLimit";
public const string Att_MangHeGoldDropWeight="Att_MangHeGoldDropWeight";
public const string Att_OffLineSellGoodsSpeed="Att_OffLineSellGoodsSpeed";
public const string AVG_6_Title="AVG/6/Title";
}

}
public class Languages {
public const string zh_CHS="zh-CHS";
public const string en="en";
 static List<string> languages = new List<string>{
zh_CHS,
en,
};
public static List<string> GetLanguages(){return languages;}
}

}