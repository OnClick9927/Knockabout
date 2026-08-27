using System.Text;
using ActionBuffer;
namespace GamePlay
{
    public interface IBackup
    {

        void ReadBackup(BufferReader reader);

        void WriteBackup(BufferWriter writer);

        void DumpString(StringBuilder builder, string perfix);

        int GetHash(ref int idx);
    }
}


