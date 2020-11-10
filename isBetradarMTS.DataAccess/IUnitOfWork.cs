
using System;

namespace isBetradarUOF.DataAccess
{
    public interface IUnitOfWork : IDisposable
    {

        void Commit();
    }
}
