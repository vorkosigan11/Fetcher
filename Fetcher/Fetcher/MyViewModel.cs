using Npgsql;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;

namespace Fetcher
{
    public class MyViewModel
    {
        public ObservableCollection<Piece> Pieces { get; set; }
        public WorkOrder WorkOrder { get; set; }

        public MyViewModel()
        {
            Pieces = new ObservableCollection<Piece>();
            WorkOrder = new WorkOrder();
        }

        private ObservableCollection<Piece> GetDataFromVendoDatabase(string connectionString)
        {
            //
            NpgsqlCommand cmd = new NpgsqlCommand();

            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    NpgsqlConnection cn = new NpgsqlConnection();
                    try
                    {
                        cn.ConnectionString = connectionString;

                        cmd.CommandText = @"SELECT
                                            P.pz_kod,
                                            P.pz_ilosc,
                                            M.ttw_value342 as NazwaZSigmy,
                                            M.ttw_value80 as NumerRysunku,
                                            M.ttw_value81 as Rewizja,
                                            K.k_kod as Firma,
                                            Sl.es_text as Typ
                                        FROM
                                            tg_planzlecenia P
                                        INNER JOIN tg_towary T ON P.ttw_idtowaru = T.ttw_idtowaru
                                        INNER JOIN mvv.tg_towary_mv M ON M.ttw_idtowaru = T.ttw_idtowaru
                                        INNER JOIN tg_elslownika SL ON SL.es_idelem = M.ttw_value29
                                        INNER JOIN tb_klient K ON K.k_idklienta = M.ttw_value68
                                        WHERE P.zl_idzlecenia IN(SELECT zl_idzlecenia FROM tg_zlecenia WHERE zl_nrzlecenia = @numerZlecenia AND  zl_rok=@rok) AND Sl.es_text = 'Blacha' AND T.tgr_idgrupy = 20";

                        cmd.Parameters.Add("@numerZlecenia", NpgsqlTypes.NpgsqlDbType.Integer);
                        cmd.Parameters["@numerZlecenia"].Value = int.Parse(WorkOrder.WorkOrderNumber.Split(new char[] { '/' }).ElementAt(0));
                        cmd.Parameters.Add("@rok", NpgsqlTypes.NpgsqlDbType.Text);
                        cmd.Parameters["@rok"].Value = WorkOrder.WorkOrderNumber.Split(new char[] { '/' }).ElementAt(2);
                        cmd.Connection = cn;

                        cn.Open();

                        using (NpgsqlDataReader sdr = cmd.ExecuteReader())
                        {
                            if (sdr.HasRows)
                            {
                                while (sdr.Read())
                                {
                                    Debug.WriteLine(sdr.ToString());
                                    Pieces.Add(new Piece(
                                        Convert.IsDBNull(sdr.GetValue(0)) ? string.Empty : sdr.GetString(0),
                                        sdr.GetInt32(1),
                                        Convert.IsDBNull(sdr.GetValue(2)) ? string.Empty : sdr.GetString(2),
                                        Convert.IsDBNull(sdr.GetValue(3)) ? string.Empty : sdr.GetString(3),
                                        Convert.IsDBNull(sdr.GetValue(4)) ? string.Empty : sdr.GetString(4),
                                        Convert.IsDBNull(sdr.GetValue(5)) ? string.Empty : sdr.GetString(5),
                                        Fetcher.Properties.Settings.Default.pathToParts,
                                        Pieces.Count + 1));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(String.Format("Connection failed Message: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        cn.Close();
                    }

                    scope.Complete();
                }

                return Pieces;
            }
            catch (TransactionAbortedException ex)
            {
                MessageBox.Show(String.Format("TransactionAbortedException Message: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new ObservableCollection<Piece>();
            }
        }

        private void GetDataFromVendoDatabaseClient(string connectionString)
        {
            //
            NpgsqlCommand cmd = new NpgsqlCommand();

            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    NpgsqlConnection cn = new NpgsqlConnection();
                    try
                    {
                        cn.ConnectionString = connectionString;

                        cmd.CommandText = @"SELECT
                                                TR.tr_longopis,
                                                TR.tr_nrobcy,
                                                TR.tr_datasprzedaz,
                                                K.k_kod
                                                FROM
                                                ""public"".tg_zlecenia AS Z
                                                INNER JOIN ""public"".tg_transakcje as TR ON Z.zl_idzlecenia = TR.zl_idzlecenia
                                                INNER JOIN tb_klient K ON K.k_idklienta = TR.k_idklienta
                                                WHERE
                                                Z.zl_idzlecenia IN(SELECT ZZ.zl_idzlecenia FROM ""public"".tg_zlecenia ZZ WHERE ZZ.zl_nrzlecenia =" + WorkOrder.GetWorkOrderNumber() + " AND ZZ.zl_rok = '" + WorkOrder.GetWorkOrderYear().ToString() + "')" +
                                                "AND TR.tr_rodzaj=30";

                        //cmd.Parameters["@numerZlecenia"].Value = int.Parse(numerZlecenia.Split(new char[] { '/' }).ElementAt(0));
                        //cmd.Parameters["@rok"].Value = numerZlecenia.Split(new char[] { '/' }).ElementAt(2);

                        cmd.Connection = cn;

                        cn.Open();

                        using (NpgsqlDataReader sdr = cmd.ExecuteReader())
                        {
                            if (sdr.HasRows)
                            {
                                while (sdr.Read())
                                {
                                    WorkOrder.Remark = Convert.IsDBNull(sdr.GetValue(0)) ? string.Empty : sdr.GetString(0);
                                    WorkOrder.ClientOrder = Convert.IsDBNull(sdr.GetValue(1)) ? string.Empty : sdr.GetString(1);
                                    WorkOrder.WorkOrderDate = (DateTime)sdr.GetDate(2);
                                    WorkOrder.ClientName = Convert.IsDBNull(sdr.GetValue(3)) ? string.Empty : sdr.GetString(3);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(String.Format("Connection failed Message: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        cn.Close();
                    }

                    scope.Complete();
                }
            }
            catch (TransactionAbortedException ex)
            {
                MessageBox.Show(String.Format("TransactionAbortedException Message: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string MakePathToWol()
        {
            return Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + WorkOrder.WorkOrderNumber.Replace("/", "_") + ".wol";
        }

        private void MakeWolFile(string path)
        {
            const string loadPart = "LOAD,PART,";
            const string setWO = "SET,WONO,";
            const string wo = "WO,";
            const string savePart = "SAVE,PART,ONSET,WONO,";
            const string multiParts = "SET, MULTIPARTQTY, ADD";
            // trzeba zmienic zapytanie aby pobrac date zamowienia
            string setDate = "SET,DATE," + WorkOrder.WorkOrderDate.ToString("yyyy/MM/dd", CultureInfo.CreateSpecificCulture("en-US"));

            try
            {
                string wolContent = "";

                wolContent += wo + WorkOrder.WorkOrderNumber + "," + Pieces[0].Firma + Environment.NewLine;
                wolContent += setDate + Environment.NewLine;
                wolContent += multiParts + Environment.NewLine;
                wolContent += savePart + WorkOrder.WorkOrderNumber + Environment.NewLine;

                foreach (Piece item in Pieces)
                {
                    wolContent += loadPart + item.Path.ToString() + "," + item.Ilosc.ToString() + Environment.NewLine;
                    wolContent += setWO + WorkOrder.WorkOrderNumber + Environment.NewLine;
                }

                // Delete the file if it exists.
                if (File.Exists(path))
                {
                    // Note that no lock is put on the
                    // file and the possibility exists
                    // that another process could do
                    // something with it between
                    // the calls to Exists and Delete.
                    File.Delete(path);
                }

                // Create the file.
                using (FileStream fs = File.Create(path))
                {
                    Byte[] info = new UTF8Encoding(true).GetBytes(wolContent);
                    // Add some information to the file.
                    fs.Write(info, 0, info.Length);
                }

                // Open the stream and read it back.
                //using (StreamReader sr = File.OpenText(path))
                //{
                //    string s = "";
                //    while ((s = sr.ReadLine()) != null)
                //    {
                //        Console.WriteLine(s);
                //    }
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void AddDataToResults(string connectionStringToVendo, string connectionStringToSigma)
        {
            Pieces.Clear();
            Pieces = GetDataFromVendoDatabase(connectionStringToVendo);
            GetDataFromVendoDatabaseClient(connectionStringToVendo);
            Parallel.For(0, Pieces.Count, index =>
              {
                  (Pieces.ElementAt(index) as Piece).Checkfile((Pieces.ElementAt(index) as Piece).Path);
              });
        }

        public Boolean AllFilesExist()
        {
            return Pieces.All(item => item.Exist == true);
        }

        public int ChangeInSigma(string connectionString, string query)
        {
            int rowAffected = 0;

            SqlConnection cn = new SqlConnection();
            cn.ConnectionString = connectionString;
            SqlCommand cmd = new SqlCommand();

            cmd.Connection = cn;
            cmd.CommandText = query;

            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    try
                    {
                        cn.Open();
                        rowAffected = cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(String.Format("Connection failed Message: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        cn.Close();
                    }

                    scope.Complete();
                }

                return rowAffected;
            }
            catch (TransactionAbortedException ex)
            {
                MessageBox.Show(String.Format("TransactionAbortedException Message: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return -1;
            }
        }

        public Boolean isWoInSigma(string connectionString)
        {
            return ((DataTable)SelectFromSigma(connectionString, makeQueryAboutWoInSigma())).Rows.Count == 1 ? true : false;
        }

        public Boolean isWoInVendo(string connectionString)
        {
            return ((DataTable)SelectFromVendo(connectionString, makeQueryAboutWoInVendo())).Rows.Count == 1 ? true : false;
        }

        private DataTable SelectFromVendo(string connectionString, string query)
        {
            NpgsqlConnection cn = new NpgsqlConnection();
            cn.ConnectionString = connectionString;
            NpgsqlDataAdapter sda = new NpgsqlDataAdapter(query, connectionString);
            DataTable dt = new DataTable();
            //try
            //{
            //using (TransactionScope scope = new TransactionScope())
            // {
            try
            {
                cn.Open();
                sda.Fill(dt);
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2146233087)
                {
                    MessageBox.Show("Menedżer trasakcji rozproszonych zdechł :)");
                }
                else
                {
                    MessageBox.Show(String.Format("Connection failed Message: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                cn.Close();
            }

            //scope.Complete();
            //}

            return dt;
            //}
            //catch (TransactionAbortedException ex)
            //{
            //    MessageBox.Show(String.Format("TransactionAbortedException Message: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    return new DataTable();
            //}
        }

        public string makeQueryAboutWoInVendo()
        {
            return $"SELECT zl_idzlecenia FROM tg_zlecenia WHERE zl_nrzlecenia = '{WorkOrder.GetWorkOrderYear()}' AND  zl_rok='{WorkOrder.GetWorkOrderYear()}'";
        }

        public string makeQueryAboutWoInSigma()
        {
            return $"SELECT * FROM WO FULL OUTER JOIN WOArchive on WOArchive.WONumber = WO.WONumber where WO.WoNumber ='{WorkOrder.WorkOrderNumber}' or WOArchive.WONumber ='{WorkOrder.WorkOrderNumber}'";
        }

        public string makeQueryUpdateWoInSigma()
        {
            return $"Update Wo SET OrderNumber ='{WorkOrder.ClientOrder}', WOData1 ='{WorkOrder.Remark}' WHERE WONumber ='{WorkOrder.WorkOrderNumber}'";
        }

        public void RunSigma()
        {
            try
            {
                var a = Marshal.GetActiveObject("SigmaNEST.SNAutomation");
                MessageBox.Show(a.ToString());
            }
            catch (Exception)
            {
            }

            Type comObjectType = Type.GetTypeFromProgID("SigmaNEST.SNAutomation", true);

            Object comObject = Activator.CreateInstance(comObjectType);

            if (comObject != null)
            {
                MakeWolFile(MakePathToWol());
                Object[] parameters = new Object[2];
                parameters[0] = MakePathToWol();
                parameters[1] = true;

                comObject.GetType().InvokeMember("RunWOLFile", BindingFlags.InvokeMethod, null, comObject, parameters);
            }
            else
            {
                MessageBox.Show("Nie znaleziono Sigmy.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public DataTable SelectFromSigma(string connectionString, string query)
        {
            SqlConnection cn = new SqlConnection();
            cn.ConnectionString = connectionString;
            SqlDataAdapter sda = new SqlDataAdapter(query, connectionString);
            DataTable dt = new DataTable();
            //try
            //{
            //using (TransactionScope scope = new TransactionScope())
            // {
            try
            {
                cn.Open();
                sda.Fill(dt);
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2146233087)
                {
                    MessageBox.Show("Menedżer trasakcji rozproszonych zdechł :)");
                }
                else
                {
                    MessageBox.Show(String.Format("Connection failed Message: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                cn.Close();
            }

            //scope.Complete();
            //}

            return dt;
            //}
            //catch (TransactionAbortedException ex)
            //{
            //    MessageBox.Show(String.Format("TransactionAbortedException Message: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    return new DataTable();
            //}
        }
    }
}