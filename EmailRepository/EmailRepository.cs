using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Routeco.EmailWorkService.Domain;
using System;
using System.Data.SqlClient;

namespace Routeco.Data.EmailRepository
{
    public class EmailRepository : IEmailRepository
    {
        private readonly string connectionString;
        private readonly ILogger<EmailRepository> logger;

        public EmailRepository(IOptions<ConnectionStrings> connections, ILogger<EmailRepository> logger)
        {
            this.connectionString = connections.Value.RequestConnection;
            this.logger = logger;
        }
        public EmailRequest Read()
        {
            logger.LogInformation("Reading message");
            using (var connection = new SqlConnection(connectionString))
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
                        logger.LogInformation("Adding message to process queue -{id}", message.Id);
                        var insert = "insert into ProcessQueue(Id, Message,TimeStamp) values (@Id,@Message, @Timestamp);";
                        connection.Execute(insert, new { message.Id, message.Message, message.TimeStamp }, transaction: transaction);
                        logger.LogInformation("Removing message from message queue -{id}", message.Id);
                        var delete = "delete from MessageQueue where id = @id";
                        var deletedRows = connection.Execute(delete, new { message.Id }, transaction: transaction);
                        if (deletedRows == 0)
                        {
                            transaction.Rollback();
                            logger.LogWarning("Unable to remove message {id} from message queue - message not found", message.Id);
                            return null;
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Exception reading message {exception}", ex.Message);
                        transaction.Rollback();
                        return null;
                    }
                }
                return message;
            }
        }

        public void Delete(int id)
        {
            logger.LogInformation("Removing message from process queue -{id}", id);
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            Delete(id, connection, transaction);
            transaction.Commit();
        }

        public void MoveToError(int id, string exception)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            MoveToError(id, exception, connection, transaction);

        }

        private void MoveToError(int id, string exception, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                InsertError(id, exception, connection, transaction: transaction);
                Delete(id, connection, transaction);
                transaction.Commit();
            }
            catch (Exception ex)
            {
                logger.LogError("Error Adding message to error queue -{id}, {exception}", id, ex.Message);
                transaction.Rollback();
            }
        }

        private void InsertError(int id, string exception, SqlConnection connection, SqlTransaction transaction)
        {
            logger.LogInformation("Adding message to error queue -{id}", id);
            var insert = "insert into ErrorQueue(Id, Message,TimeStamp,Exception) select id, message, TimeStamp, @exception from processQueue where id =@id";
            connection.Execute(insert, new { id, exception }, transaction);
        }

        private void Delete(int id, SqlConnection connection, SqlTransaction transaction)
        {
            logger.LogInformation("Deleting message from process queue -{id}", id);
            var delete = "delete from processQueue where id = @id";
            var deletedRows = connection.Execute(delete, new { id }, transaction: transaction);
            if (deletedRows == 0)
            {
                logger.LogError("Deleting message from process queue -{id}", id);
                var insert = "insert into ErrorQueue(Id, TimeStamp,Exception) values(@id,@dateTime, @error)";
                connection.Execute(insert, new { id, dateTime = DateTime.UtcNow, error = "Message missing from process queue" }, transaction);
            }
        }
    }
}
