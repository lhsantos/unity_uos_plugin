using System.Collections.Generic;


namespace UOS
{
    public interface UOSDriver
    {
        UpDriver GetDriver();

        List<UpDriver> GetParent();

        void Init(IGateway gateway, uOSSettings settings, string instanceId);

        void Destroy();
    }
}
