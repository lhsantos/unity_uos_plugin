namespace UOS
{
    public interface UOSApplication
    {
        void Init(IGateway gateway, uOSSettings settings);

        void TearDown();
    }
}
