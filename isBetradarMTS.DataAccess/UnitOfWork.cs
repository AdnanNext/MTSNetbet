using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using isBetradarMTS.DataAccess.Repositories;
using log4net;
using MySql.Data.MySqlClient;

namespace isBetradarUOF.DataAccess
{
    public class UnitOfWork : IUnitOfWork
    {
        private IDbConnection _connection;
        private IDbTransaction _transaction;
        private ILog _log;
        private bool _disposed;

        //configurations
        private ITicketRepository _ticketRepository;

        public UnitOfWork(string connectionString)
        {
            _connection = new MySqlConnection(connectionString);
            _connection.Open();
            _transaction = _connection.BeginTransaction();
        }
        public UnitOfWork(string connectionString, ILog logger)
        {
            _log = logger;
            _connection = new MySqlConnection(connectionString);
            _connection.Open();
            _transaction = _connection.BeginTransaction();
        }

        //public properties
        #region Tickets 
        public ITicketRepository TicketRepository
        {
            get { return _ticketRepository ?? (_ticketRepository = new TicketRepository(_transaction, _log)); }
        }
        #endregion
        //

        public void Commit()
        {
            try
            {
                try
                {
                    _transaction.Commit();
                }
                catch
                {
                    _transaction.Rollback();
                }
                finally
                {
                    _transaction.Dispose();
                    //_transaction = _connection.BeginTransaction();
                    ResetRepositories();
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error in commit(): " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        private void ResetRepositories()
        {
            //tickets
            _ticketRepository = null;
        }
        public void Dispose()
        {
            dispose(true);
            GC.SuppressFinalize(this);
        }
        private void dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_transaction != null)
                    {
                        _transaction.Dispose();
                        _transaction = null;
                    }
                    if (_connection != null)
                    {
                        _connection.Dispose();
                        _connection.Close();
                        _connection = null;
                    }
                }
                _disposed = true;
            }
        }
        ~UnitOfWork()
        {
            dispose(false);
        }
    }
}
