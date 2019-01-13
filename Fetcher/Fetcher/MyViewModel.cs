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

namespace Fetcher
{
    public class MyViewModel
    {
        public ObservableCollection<Twor> Results { get; set; }

        private string connString = "";

        public MyViewModel()
        {
            Results = new ObservableCollection<Twor>();
            //AddDataToResults("DD");
        }

        public void AddDataToResults(string woNumber, string rok, string connectionString)
        {
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
    }
}