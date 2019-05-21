using DLT;
using DLT.Meta;
using System;
using System.Collections.Generic;
using System.Text;

namespace SPIXI.Storage
{
    class TransactionCache
    {
        public static List<Transaction> transactions = new List<Transaction> { };
        public static List<Transaction> unconfirmedTransactions = new List<Transaction> { };

        public static ulong lastChange = 0;

        // Retrieve a transaction from local storage
        // Todo: if transaction not found in local storage, send a network-wide request
        public static Transaction getTransaction(string txid)
        {
            // First check the confirmed transactions cache
            lock (transactions)
            {
                foreach (Transaction tx in transactions)
                {
                    if (txid.Equals(tx.id, StringComparison.Ordinal))
                        return tx;
                }
            }

            return null;
        }

        // Retrieves an unconfirmed transaction if found in local storage
        public static Transaction getUnconfirmedTransaction(string txid)
        {
            // Check also in unconfirmed transactions
            lock (unconfirmedTransactions)
            {
                foreach (Transaction tx in unconfirmedTransactions)
                {
                    if (txid.Equals(tx.id, StringComparison.Ordinal))
                        return tx;
                }
            }
            return null;
        }

        // Add a transaction to local storage
        public static bool addTransaction(Transaction transaction, bool writeToFile = true)
        {
            lock (transactions)
            {
                // Check if transaction id is already in local storage
                Transaction cached_tx = null;
                foreach (Transaction tx in transactions)
                {
                    if (transaction.id.Equals(tx.id, StringComparison.Ordinal))
                    {
                        cached_tx = tx;
                        break;
                    }
                }


                // Remove old cached transaction from local storage
                if(cached_tx != null)
                {
                    transactions.Remove(cached_tx);
                }
                
                // Remove from unconfirmed transactions if found
                {
                    cached_tx = null;
                    foreach (Transaction tx in unconfirmedTransactions)
                    {
                        if (transaction.id.Equals(tx.id, StringComparison.Ordinal))
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

                // Add new transaction to local storage
                transactions.Add(transaction);

            }
            // Write to file
            if(writeToFile)
                Node.localStorage.writeTransactionCacheFile();

            lastChange++;
            if (lastChange > 100000)
                lastChange = 0;

            return true;
        }

        // Add an unconfirmed transaction
        public static bool addUnconfirmedTransaction(Transaction transaction, bool writeToFile = true)
        {
            lock(unconfirmedTransactions)
            {
                // Check if transaction id is already in local storage
                Transaction cached_tx = null;
                foreach (Transaction tx in unconfirmedTransactions)
                {
                    if (transaction.id.Equals(tx.id, StringComparison.Ordinal))
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
                unconfirmedTransactions.Add(transaction);
            }
            // Write to file
            if(writeToFile)
                Node.localStorage.writeTransactionCacheFile();

            lastChange++;
            if (lastChange > 100000)
                lastChange = 0;

            return true;
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

            lastChange++;
            if (lastChange > 100000)
                lastChange = 0;
        }
    }
}
