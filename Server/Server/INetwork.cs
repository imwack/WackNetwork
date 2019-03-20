
namespace Server
{
    public interface INetwork
    {
        void OneLoop();
        void Close();
    }
}
