using Dapper;
using Microsoft.Extensions.Options;
using Routeco.EmailWorkService.Domain;
using System;
using System.Data.SqlClient;
using System.Net.Cache;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Transactions;

namespace Routeco.Data.EmailRepository
{
    public class EmailRepository : IEmailRepository
    {
        private readonly string requestConnection;

        public EmailRepository(IOptions<ConnectionStrings> connections)
        {
            this.requestConnection = connections.Value.RequestConnection;
        }
        public EmailRequest Read()
        {
            using (var connection = new SqlConnection(requestConnection))
            {
                connection.Open();
                EmailRequest message;
                using (var transaction = connection.BeginTransaction())
                {
                    var read = "Select Id, Message, TimeStamp from MessageQueue order by timestamp desc";
                    try
                    {
                        message = connection.QueryFirstOrDefault<EmailRequest>(read, transaction: transaction);
                        if (message is null) return null;
                        var insert = "insert into ProcessQueue(Id, Message,TimeStamp) values (@Id,@Message, @Timestamp);";
                        connection.Execute(insert, new { message.Id, Message = message.Message, message.TimeStamp }, transaction: transaction);
                        var delete = "delete from MessageQueue where id = @id";
                        var deletedRows = connection.Execute(delete, new { message.Id }, transaction: transaction);
                        if (deletedRows == 0)
                        {
                            transaction.Rollback();
                            return null;
                        }
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        return null;
                    }
                }
                return message;
            }
        }

        public void Delete(int id)
        {
            using var connection = new SqlConnection(requestConnection);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            Delete(id, connection, transaction);
        }

        private static void Delete(int id, SqlConnection connection, SqlTransaction transaction)
        {
            var delete = "delete from processQueue where id = @id";
            var deletedRows = connection.Execute(delete, new { id }, transaction: transaction);
            if (deletedRows == 0)
            {
                throw new Exception("Whoops");
            }
        }

        public void MoveToError(int id, string exception)
        {
            using var connection = new SqlConnection(requestConnection);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            MoveToError(id, connection, transaction);

        }

        private static void MoveToError(int id, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                InsertError(id, connection, transaction: transaction);
                Delete(id, connection, transaction: transaction);
                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                //Todo: failed to move
                throw;
            }
        }

        private static void InsertError(int id, SqlConnection connection, SqlTransaction transaction)
        {
            var insert = "insert into ErrorQueue(Message,TimeStamp,Exception) select message, @exception, datetime from processQueue where id =@id";
            connection.Execute(insert, new { id }, transaction);
        }
    }
}
