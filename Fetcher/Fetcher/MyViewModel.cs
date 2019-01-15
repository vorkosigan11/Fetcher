using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using System.Data;
using System.Transactions;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Diagnostics;
using System.Reflection;

namespace Fetcher
{

    public class MyViewModel
    {
        public ObservableCollection<Twor> Results { get; set; }

        private string connString = "";

        private string numerZlecenia = "";

        public MyViewModel()
        {
            Results = new ObservableCollection<Twor>();
            //AddDataToResults("DD");
        }

        public void AddDataToResults(string woNumber, string rok, string connectionString)
        {
            numerZlecenia = woNumber + "/SW/" + rok + "/ZLP";

            connString = connectionString;
            Results.Clear();
            Results = GetDataFromVendoDatabase(woNumber as string, rok as string);
            Parallel.For(0, Results.Count, index =>
              {
                  (Results.ElementAt(index) as Twor).Checkfile((Results.ElementAt(index) as Twor).Path);
              });
        }

        private ObservableCollection<Twor> GetDataFromVendoDatabase(string woNumber, string rok)
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
                        cn.ConnectionString = connString;

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
                        cmd.Parameters["@numerZlecenia"].Value = int.Parse(woNumber);
                        cmd.Parameters.Add("@rok", NpgsqlTypes.NpgsqlDbType.Text);
                        cmd.Parameters["@rok"].Value = rok;
                        cmd.Connection = cn;

                        cn.Open();

                        using (NpgsqlDataReader sdr = cmd.ExecuteReader())
                        {
                            if (sdr.HasRows)
                            {
                                while (sdr.Read())
                                {
                                    Debug.WriteLine(sdr.ToString());
                                    Results.Add(new Twor(
                                        Convert.IsDBNull(sdr.GetValue(0)) ? string.Empty : sdr.GetString(0),
                                        sdr.GetInt32(1),
                                        Convert.IsDBNull(sdr.GetValue(2)) ? string.Empty : sdr.GetString(2),
                                        Convert.IsDBNull(sdr.GetValue(3)) ? string.Empty : sdr.GetString(3),
                                        Convert.IsDBNull(sdr.GetValue(4)) ? string.Empty : sdr.GetString(4),
                                        Convert.IsDBNull(sdr.GetValue(5)) ? string.Empty : sdr.GetString(5),
                                        Fetcher.Properties.Settings.Default.pathToParts,
                                        Results.Count + 1));
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

                return Results;
            }
            catch (TransactionAbortedException ex)
            {
                MessageBox.Show(String.Format("TransactionAbortedException Message: {0}", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new ObservableCollection<Twor>();
            }
        }

        public void RunSigma()
        {
            Type comObjectType = Type.GetTypeFromProgID("SigmaNEST.SNAutomation", true);
            Object comObject = Activator.CreateInstance(comObjectType);

            makeWol(makePathToWol());
            Object[] parameters = new Object[2];
            parameters[0] = makePathToWol();
            parameters[1] = true;

           comObject.GetType().InvokeMember("RunWOLFile", BindingFlags.InvokeMethod, null, comObject, parameters);
        }

        private string makePathToWol()
        {
            return Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + numerZlecenia.Replace("/", "_") + ".wol";
        }

        private void makeWol(string path)
        {
            const string loadPart = "LOAD,PART,";
            const string setWO = "SET,WONO,";
            const string wo = "WO,";
            const string savePart = "SAVE,PART,ONSET,WONO,";
            // trzeba zmienic zapytanie aby pobrac date zamowienia
            const string setDate = "SET,DATE,2019/01/18";



            try
            {
                string wolContent = "";

                wolContent += wo + numerZlecenia + "," + Results[1].Firma + Environment.NewLine;
                wolContent += setDate + Environment.NewLine;
                wolContent += savePart + numerZlecenia + Environment.NewLine;

                foreach (Twor item in Results)
                {
                    wolContent += loadPart + item.Path.ToString() + "," + item.Ilosc.ToString() + Environment.NewLine;
                    wolContent += setWO + numerZlecenia + Environment.NewLine;
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
    }
}