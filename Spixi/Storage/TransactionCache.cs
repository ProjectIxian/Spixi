using IXICore;
using IXICore.Meta;
using Spixi.Storage.Models;
using SPIXI.Meta;

namespace SPIXI.Storage
{
    class TransactionCache
    {
        public static List<StorageTransaction> transactions = new List<StorageTransaction> { };
        
        public static List<StorageTransaction> unconfirmedTransactions = new List<StorageTransaction> { };

        public static ulong lastChange = 0;

        // Retrieve a transaction from local storage
        // Todo: if transaction not found in local storage, send a network-wide request
        public static Transaction getTransaction(byte[] txid)
        {
            // First check the confirmed transactions cache
            lock (transactions)
            {
                foreach (StorageTransaction tx in transactions)
                {
                    if (txid.SequenceEqual(tx.transaction.id))
                        return tx.transaction;
                }
            }

            return null;
        }

        // Retrieves an unconfirmed transaction if found in local storage
        public static Transaction getUnconfirmedTransaction(byte[] txid)
        {
            // Check also in unconfirmed transactions
            lock (unconfirmedTransactions)
            {
                foreach (StorageTransaction tx in unconfirmedTransactions)
                {
                    if (txid.SequenceEqual(tx.transaction.id))
                        return tx.transaction;
                }
            }
            return null;
        }

        // Add a storage transaction to local storage
        public static bool addTransaction(StorageTransaction t, bool writeToFile = true)
        {
            lock (transactions)
            {
                // Check if transaction id is already in local storage
                StorageTransaction cached_tx = null;
                foreach (StorageTransaction tx in transactions)
                {
                    if (t.transaction.id.SequenceEqual(tx.transaction.id))
                    {
                        cached_tx = tx;
                        break;
                    }
                }

                // Remove old cached transaction from local storage
                if (cached_tx != null)
                {
                    transactions.Remove(cached_tx);
                }

                // Remove from unconfirmed transactions if found
                lock (unconfirmedTransactions)
                {
                    cached_tx = null;
                    foreach (StorageTransaction tx in unconfirmedTransactions)
                    {
                        if (t.transaction.id.SequenceEqual(tx.transaction.id))
                        {
                            cached_tx = tx;
                            break;
                        }
                    }
                    if (cached_tx != null)
                    {
                        unconfirmedTransactions.Remove(cached_tx);
                    }
                }
            }
            // Add new transaction to local storage
            transactions.Add(t);

            // TODO improve this when/if needed
            // keep only last 1000 transactions
            if (transactions.Count > 1000)
            {
                transactions.RemoveAt(0);
            }
        
            // Write to file
            if(writeToFile)
                Node.localStorage.writeTransactionCacheFile();

            updateCacheChangeStatus();

            return true;
        }


        // Add a transaction to local storage
        public static bool addTransaction(Transaction transaction, bool writeToFile = true)
        {
            return addTransaction(new StorageTransaction(transaction), writeToFile);
        }

        // Add an unconfirmed storage transaction to local storage
        public static bool addUnconfirmedTransaction(StorageTransaction t, bool writeToFile = true)
        {
            lock (unconfirmedTransactions)
            {
                StorageTransaction cached_tx = null;
                foreach (StorageTransaction tx in unconfirmedTransactions)
                {
                    if (t.transaction.id.SequenceEqual(tx.transaction.id))
                    {
                        cached_tx = tx;
                        break;
                    }
                }

                // Remove old cached transaction from local storage
                if (cached_tx != null)
                {
                    unconfirmedTransactions.Remove(cached_tx);
                }

                // Add new transaction to local storage
                unconfirmedTransactions.Add(t);

                // TODO improve this when/if needed
                // keep only last 1000 transactions
                if (unconfirmedTransactions.Count > 1000)
                {
                    unconfirmedTransactions.RemoveAt(0);
                }
            }
            // Write to file
            if (writeToFile)
                Node.localStorage.writeTransactionCacheFile();

            updateCacheChangeStatus();

            return true;
        }

        // Add an unconfirmed transaction
        public static bool addUnconfirmedTransaction(Transaction transaction, bool writeToFile = true)
        {
            return addUnconfirmedTransaction(new StorageTransaction(transaction), writeToFile);
        }

        // Clears all transactions from memory
        public static void clearAllTransactions()
        {
            lock (transactions)
            {
                transactions.Clear();
            }

            lock (unconfirmedTransactions)
            {
                unconfirmedTransactions.Clear();
            }

            updateCacheChangeStatus();
        }

        // Updates the last change status of the Transaction Cache
        public static void updateCacheChangeStatus()
        {
            lastChange++;
            if (lastChange > 100000)
                lastChange = 0;
        }
    }
}
