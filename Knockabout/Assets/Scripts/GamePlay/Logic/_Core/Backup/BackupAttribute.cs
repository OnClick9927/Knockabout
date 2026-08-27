using System;

namespace GamePlay
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property,Inherited =false)]
    public class BackupAttribute : System.Attribute {

        public bool CustomCreateElement = false;
    }


}


