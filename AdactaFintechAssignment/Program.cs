using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using MySql.Data.MySqlClient;

namespace AdactaFintechAssignment
{
    class Program
    {
        static MySqlConnection _connection;
        static void Main(string[] args)
        {
            _connection = new MySqlConnection("server=localhost;database=adactafintechassignment;uid=NaiveUser;pwd=MyPassword123!;");
            try
            {
                _connection.Open();

                // 1. Iz podatkovne baze prebere seznam znakovnih nizov po primarnem ključu.
                GetAll();

                Stopwatch stopWatch = new Stopwatch();

                stopWatch.Start(); 
                // 2. Iz podatkovne baze prebere isti seznam nizov, ki je urejen naraščajoče (urejanje izvede podatkovna baza).
                var sortedList = GetAllASC();
                stopWatch.Stop();

                // 4. Izmeri čas potreben za izvedbo točke 2 na 1 ms natančno.
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);


                stopWatch.Start();
                var unosrtedList = GetAll();

                // 3. Napiši funkcijo, ki uporabi enega od poznanih algoritmov za urejanje (npr. urejanje z mehurčki) in uredi seznam znakovnih nizov iz točke 1.naraščajoče.
                BubbleSort(ref unosrtedList);

                // 5. Izmeri čas potreben za izvedbo točke 3 na 1 ms natančno.
                stopWatch.Stop();
                ts = stopWatch.Elapsed;
                elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);

                // 6. Opcijsko v dodaten stolpec tabele v podatkovni bazi vsakemu znakovnemu nizu zapiši rang (vrstni red).
                UpdateTable(unosrtedList);

                // 7. Opcijsko opiši, kakšna je časovna zahtevnost algoritma za sortiranje, ki si ga uporabil v svojem programu.
                /*
                 * Uporabil sem buble sort, algoritem sicer ni najhitrejši je pa malo bolj pregleden in preprostejši za razlago kot naprimer Block sort (https://en.wikipedia.org/wiki/Block_sort)
                 * Best case uporabljenga buble sorta je n, kjer je n število zapisov in sicer v primero ko je seznam že sortiran.
                 * Worst case zahtevnost je n^2 oz l*n^2, kjer je n število zapisov in l dolžina nizov (uporabljena pri primerjanju).
                 */
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (_connection != null)
                {
                    _connection.Close();
                }
            }
        }

        static TableWithStringsEntity[] GetAll()
        {
            List<TableWithStringsEntity> results = new List<TableWithStringsEntity>();
            MySqlCommand cmd = new MySqlCommand("SELECT * FROM tablewithstrings", _connection);
            MySqlDataReader reader = cmd.ExecuteReader();
            try
            {
                if (_connection != null)
                {
                    while (reader.Read())
                    {
                        results.Add(new TableWithStringsEntity(reader.GetInt32(0), reader.GetString(1)));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }
            return results.ToArray();
        }

        static TableWithStringsEntity[] GetAllASC()
        {
            List<TableWithStringsEntity> results = new List<TableWithStringsEntity>();
            MySqlCommand cmd = new MySqlCommand("SELECT * FROM tablewithstrings ORDER BY RandomString ASC", _connection);
            MySqlDataReader reader = cmd.ExecuteReader();
            try
            {
                if (_connection != null)
                {
                    while (reader.Read())
                    {
                        results.Add(new TableWithStringsEntity(reader.GetInt32(0), reader.GetString(1)));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }
            return results.ToArray();
        }

        // Source: https://en.wikipedia.org/wiki/Bubble_sort
        static void BubbleSort(ref TableWithStringsEntity[] list)
        {
            var n = list.Length;
            do
            {
                var newn = 0;
                for (int i = 1; i < n; i++)
                {
                    if (StringCompare(list[i - 1].RandomString, list[i].RandomString) > 0)
                    {
                        Swap(i, ref list);
                        newn = i;
                    }
                }
                n = newn;
            } while (n >= 1);
        }

        static void Swap(int i, ref TableWithStringsEntity[] list)
        {
            var temp = list[i];
            list[i] = list[i - 1];
            list[i - 1] = temp;
        }

        static int StringCompare(string a, string b)
        {
            if (String.IsNullOrEmpty(a) || String.IsNullOrEmpty(b)) throw new ArgumentNullException();

            int i = 0;
            int aLength = a.Length;
            int bLength = b.Length;

            while (i < aLength && i < bLength)
            {
                if (a[i] == b[i])
                {
                    i++;
                    continue;
                }
                return a[i].CompareTo(b[i]);
            }

            if (aLength < bLength)
                return 1;
            else if (aLength > bLength)
                return -1;

            return 0;
        }


        static void PrintList(TableWithStringsEntity[] list)
        {
            if (list == null) throw new ArgumentNullException("list");

            foreach (var item in list)
            {
                Console.WriteLine(item.Id + ": " + item.RandomString);
            }
        }

        static void UpdateTable(TableWithStringsEntity[] list)
        {
            if (_connection == null) throw new Exception("connection closed");
            
            MySqlCommand cmd = new MySqlCommand("UPDATE tablewithstrings SET Rang=@i WHERE Id=@id", _connection);
            MySqlTransaction trans;
            trans = _connection.BeginTransaction();
            try
            {                
                cmd.Transaction = trans;

                MySqlParameter id = new MySqlParameter("@id", SqlDbType.Int);
                MySqlParameter i = new MySqlParameter("@i", SqlDbType.Int);
                cmd.Parameters.Clear();
                cmd.Parameters.Add(id);
                cmd.Parameters.Add(i);

                int index = 1;
                foreach (var item in list)
                {
                    id.Value = item.Id;
                    i.Value = index++;
                    cmd.ExecuteNonQuery();
                }

                trans.Commit();
            }
            catch (Exception ex)
            {
                try
                {
                    trans.Rollback();
                }
                catch (SqlException sqlEx)
                {
                    if (trans.Connection != null)
                    {
                        Console.WriteLine("An exception of type " + sqlEx.GetType() +
                        " was encountered while attempting to roll back the transaction.");
                    }
                }

                Console.WriteLine("An exception of type " + ex.GetType() + " was encountered while inserting the data.");
                Console.WriteLine("Neither record was written to database.");
            }
        }
    }

    struct TableWithStringsEntity
    {
        public int Id;
        public string RandomString;

        public TableWithStringsEntity(int id, string randomString)
        {
            Id = id;
            RandomString = randomString;
        }
    }
}
