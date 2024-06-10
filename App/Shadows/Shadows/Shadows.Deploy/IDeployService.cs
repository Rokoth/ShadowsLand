using System.Threading.Tasks;

namespace Shadows.Deploy
{
    public interface IDeployService
    {
        Task Deploy(int? num = null);
    }
}
