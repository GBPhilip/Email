using Routeco.EmailWorkService.Domain;
using System.Threading.Tasks;

namespace Routeco.Data.EmailRepository
{
    public interface IEmailRepository
    {
        void Delete(int id);
        void MoveToError(int id, string exception);
        EmailRequest Read();
    }
}