﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PowerNetwork.Core.DataModels;
using Npgsql;
using NpgsqlTypes;

namespace PowerNetwork.Core.Helpers
{
    public class DataService
    {

        private readonly string _connectionString = "";
        private static DataService _instance;

        public static DataService Instance(string connectionString)
        {
            _instance = _instance ?? (new DataService(connectionString));
            return _instance;
        }
        private DataService(string connectionString)
        {
            this._connectionString = connectionString;
        }

        public List<MeterModel> Meters(string groupCode)
        {
            var result = new List<MeterModel>();

            using (var conn = new NpgsqlConnection(this._connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"select m.cups, m.tipo_punto 
                    from coordenadas_acometida g
                    join datos_geograficos_cups m on g.cups = m.cups
                    where g.clave_acometida = ?";
                    cmd.Parameters.Add("GroupCode", NpgsqlDbType.Double).Value = double.Parse(groupCode);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            var model = new MeterModel { Code = reader["cups"] as string, Type = reader["tipo_punto"] as string };
                            result.Add(model);
                        }
                    }
                }
            }


            return result;
        }

        public List<MeterGroupModel> MeterGroups(string otherCode)
        {
            var result = new List<MeterGroupModel>();

            using (var connection = new NpgsqlConnection(this._connectionString))
            {
                using (var command = new NpgsqlCommand(
                    @"select clave_acometida, log_sexadecimal, lat_sexadecimal, matricula_salidabt 
                    from coordenadas_acometida where matricula_ct = ?", connection))
                {
                    command.Parameters.Add("OtherCode", NpgsqlDbType.Varchar).Value = otherCode;

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {

                        while (reader.Read())
                        {
                            var model = new MeterGroupModel
                            {
                                Code = ((double)reader["clave_acometida"]).ToString(),
                                Lng = (double)reader["log_sexadecimal"],
                                Lat = (double)reader["lat_sexadecimal"],
                                Exit = reader.IsDBNull(3) ? "" : reader["matricula_salidabt"] as string
                            };
                            if (result.All(o => o.Code != model.Code)) result.Add(model);
                        }
                    }


                }
            }

            return result;
        }

        public string CtFromMeterCode(string meterCode)
        {
            var result = "";

            using (var connection = new NpgsqlConnection(this._connectionString))
            {
                using (var command = new NpgsqlCommand("select top 1 matricula_ct from datos_geograficos_cups where cups like ?", connection))
                {
                    command.Parameters.Add("MeterCode", NpgsqlDbType.Varchar).Value = "%" + meterCode + "%";

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result = reader["matricula_ct"] as string;
                        }

                    }
                }
            }

            return result;
        }

        public List<AlarmModel> OverloadAlarms()
        {
            var result = new List<AlarmModel>();

            using (var connection = new NpgsqlConnection(this._connectionString))
            {
                using (var command = new NpgsqlCommand(
                     @"select matricula, ratio, potencia_instaladatotal, intervalo 
                    from a1_balance_alarma_final_prueba1 order by ratio desc", connection))
                {

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var model = new AlarmModel
                            {
                                Code = reader["matricula"] as string,
                                Ratio = (double)((decimal)reader["ratio"]),
                                Data = new[] {
                            reader.IsDBNull(2) ? "" : ((double)reader["potencia_instaladatotal"]).ToString(CultureInfo.InvariantCulture),
                            reader["intervalo"] as string
                        }
                            };

                            result.Add(model);
                        }

                    }
                }
            }

            return result;
        }

        public List<AlarmModel> UnbalanceAlarms()
        {
            var result = new List<AlarmModel>();

            using (var connection = new NpgsqlConnection(this._connectionString))
            {
                using (var command = new NpgsqlCommand(
                       @"select matricula, dif_prc_hora_desbalaceo_sobre_nominal 
                    from balance_alarma_desbalanceo_final 
                    order by dif_prc_hora_desbalaceo_sobre_nominal desc", connection))
                {

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var model = new AlarmModel { Code = reader["matricula"] as string, Ratio = (double)reader["dif_prc_hora_desbalaceo_sobre_nominal"] };
                            result.Add(model);
                        }
                    }
                }
            }

            return result;
        }

        public List<AlarmModel> FraudAlarms()
        {
            var result = new List<AlarmModel>();

            using (var connection = new NpgsqlConnection(this._connectionString))
            {
                using (var command = new NpgsqlCommand(
                       @"select matricula, med_dif_energia_ct_sal
                    from tabla_diferencias_fraude order by med_dif_energia_ct_sal desc", connection))
                {

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var model = new AlarmModel { Code = reader["matricula"] as string, Ratio = (double)reader["med_dif_energia_ct_sal"] };
                            result.Add(model);
                        }

                    }
                }
            }

            return result;
        }

        public List<IntensityModel> Intensity(string code, DateTime from, DateTime to, string exit)
        {
            var result = new List<IntensityModel>();

            using (var connection = new NpgsqlConnection(this._connectionString))
            {
                using (var command = new NpgsqlCommand())
                {
                    command.Connection = connection;


                    if (exit == "max")
                    {
                        command.CommandText =
                            @"select fecha, horas, fase_r, fase_s, fase_t 
                        from a1_curva_intesidad_sobrecarga 
                        where matricula = ? and fecha >= ? and fecha <= ? and tpo_salida_max = 1";

                        command.Parameters.Add("Code", NpgsqlDbType.Varchar).Value = code;
                        command.Parameters.Add("From", NpgsqlDbType.Timestamp).Value = from;
                        command.Parameters.Add("To", NpgsqlDbType.Timestamp).Value = to;

                    }
                    else
                    {
                        command.CommandText =
                            @"select fecha, horas, fase_r, fase_s, fase_t 
                        from a1_curva_intesidad_sobrecarga 
                        where matricula = ? and fecha >= ? and fecha <= ? and salida = ?";

                        command.Parameters.Add("Code", NpgsqlDbType.Varchar).Value = code;
                        command.Parameters.Add("From", NpgsqlDbType.Timestamp).Value = from;
                        command.Parameters.Add("To", NpgsqlDbType.Timestamp).Value = to;
                        command.Parameters.Add("Exit", NpgsqlDbType.Varchar).Value = exit.ToUpperInvariant();
                    }

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.IsDBNull(1)) continue;

                            var model = new IntensityModel
                            {
                                Date = (DateTime)reader["fecha"],
                                Hour = (int)reader["horas"],
                                R = reader.IsDBNull(2) ? 0 : (double)reader["fase_r"],
                                S = reader.IsDBNull(3) ? 0 : (double)reader["fase_s"],
                                T = reader.IsDBNull(4) ? 0 : (double)reader["fase_t"]
                            };

                            // TODO: temp fix for incorrect horas from db
                            if (model.Hour < 24) result.Add(model);
                        }

                    }
                }
            }

            return result;
        }

        public List<HistogramModel> Histogram(string code, DateTime from, DateTime to, string exit)
        {
            var result = new List<HistogramModel>();

            using (var connection = new NpgsqlConnection(this._connectionString))
            {
                using (var command = new NpgsqlCommand())
                {
                    command.Connection = connection;

                    if (exit == "max")
                    {
                        command.CommandText = @"select fecha, fase, prc_nominal, num_horas
                        from a1_balance_prc_nominal_bcg_04_prueba1 
                        where matricula = ? and fecha >= ? and fecha <= ? and fase <> 'TODAS FASES' and tpo_salida_max = 1";

                        command.Parameters.Add("Code", NpgsqlDbType.Varchar).Value = code;
                        command.Parameters.Add("From", NpgsqlDbType.Timestamp).Value = from;
                        command.Parameters.Add("To", NpgsqlDbType.Timestamp).Value = to;

                    }
                    else
                    {
                        command.CommandText =
                            @"select fecha, fase, prc_nominal, num_horas
                        from a1_balance_prc_nominal_bcg_04_prueba1 
                        where matricula = ? and fecha >= ? and fecha <= ? and fase <> 'TODAS FASES' and salida = ?";

                        command.Parameters.Add("Code", NpgsqlDbType.Varchar).Value = code;
                        command.Parameters.Add("From", NpgsqlDbType.Timestamp).Value = from;
                        command.Parameters.Add("To", NpgsqlDbType.Timestamp).Value = to;
                        command.Parameters.Add("Exit", NpgsqlDbType.Varchar).Value = exit.ToUpperInvariant();
                    }

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3)) continue;

                            var model = new HistogramModel
                            {
                                Date = (DateTime)reader["fecha"],
                                Fase = (string)reader["fase"],
                                Threshold = Math.Round((double)reader["prc_nominal"]),
                                Count = (long)reader["num_horas"]
                            };
                            result.Add(model);
                        }
                    }
                }
            }

            return result;
        }

        public string MaxExit(string code)
        {
            var result = string.Empty;

            using (var connection = new NpgsqlConnection(this._connectionString))
            {
                using (var command = new NpgsqlCommand(
                     @"select top 1 salida
                    from a1_balance_prc_nominal_bcg_04_prueba1 
                    where matricula = ? and tpo_salida_max = 1"))
                {

                    command.Parameters.Add("Code", NpgsqlDbType.Varchar).Value = code;

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.IsDBNull(0)) continue;
                            result = reader["salida"] as string;
                        }

                    }
                }
            }

            return result;
        }

        public List<FraudModel> Fraud(string code, DateTime from, DateTime to)
        {
            var result = new List<FraudModel>();

            using (var connection = new NpgsqlConnection(this._connectionString))
            {
                using (var command = new NpgsqlCommand(
                    @"select fecha, energia_g03, sum_energia_imp_salidas 
                    from grafica_serie_fraude 
                    where matricula = ? and fecha >= ? and fecha <= ?
                    order by fecha", connection))
                {

                    command.Parameters.Add("Code", NpgsqlDbType.Varchar).Value = code;
                    command.Parameters.Add("From", NpgsqlDbType.Timestamp).Value = from;
                    command.Parameters.Add("To", NpgsqlDbType.Timestamp).Value = to;

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.IsDBNull(1) || reader.IsDBNull(2)) continue;

                            var model = new FraudModel
                            {
                                Date = (DateTime)reader["fecha"],
                                Ct = (long)reader["energia_g03"],
                                Exit = (double)reader["sum_energia_imp_salidas"]
                            };

                            result.Add(model);
                        }
                    }

                }
            }

            return result;
        }
    }
}