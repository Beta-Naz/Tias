using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using TIAS.Core.Base;
using TIAS.Core.Enums;
using TIAS.Core.Factory;
using TIAS.Core.Models;
using TIAS.Core.Structure;
using TIAS.Models;

namespace TIAS.Core.Database
{
    public class MapDatabase
    {
        private string connectionString;
        private UnitFactory _unitFactory;

        public MapDatabase(string server, string database, string userId, string password)
        {
            connectionString = $"Server={server};Database={database};Uid={userId};Pwd=;";
            _unitFactory = new UnitFactory();
        }

        public List<HexMap> LoadAllMaps()
        {

            var maps = new List<HexMap>();

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string mapQuery = "SELECT * FROM Maps WHERE is_active = TRUE";
                using (var command = new MySqlCommand(mapQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var map = new HexMap
                        (
                            reader.GetInt32("id"),
                            reader.GetInt32("width"),
                            reader.GetInt32("height")
                        );
                        maps.Add(map);
                    }
                }

                foreach (var map in maps)
                {
                    LoadMapCells(connection, map);
                    LoadMapUnits(connection, map);
                }
            }

            return maps;
        }

        private void LoadMapCells(MySqlConnection connection, HexMap map)
        {
            string query = @"
                SELECT mc.pos_q, mc.pos_r, ct.type_name 
                FROM MapCells mc
                JOIN CellTypes ct ON mc.cell_type_id = ct.id
                WHERE mc.map_id = @mapId";

            map.Cells = new CellType[map.Width, map.Height];

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@mapId", map.Id);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int q = reader.GetInt32("pos_q");
                        int r = reader.GetInt32("pos_r");
                        string typeName = reader.GetString("type_name");

                        if (q < map.Width && r < map.Height)
                        {
                            map.Cells[q, r] = (CellType)Enum.Parse(typeof(CellType), typeName);
                        }
                    }
                }
            }
        }

        private void LoadMapUnits(MySqlConnection connection, HexMap map)
        {
            string query = @"
                SELECT mu.pos_q, mu.pos_r, u.unit_type, u.alliance, u.id, mu.current_health
                FROM MapUnits mu
                JOIN Units u ON mu.unit_id = u.id
                WHERE mu.map_id = @mapId";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@mapId", map.Id);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int q = reader.GetInt32("pos_q");
                        int r = reader.GetInt32("pos_r");
                        string unitTypeStr = reader.GetString("unit_type");
                        string allianceStr = reader.GetString("alliance");
                        int id = reader.GetInt32("id");

                        UnitType unitType = (UnitType)Enum.Parse(typeof(UnitType), unitTypeStr);
                        TypeAlliance alliance = (TypeAlliance)Enum.Parse(typeof(TypeAlliance), allianceStr);
                        var unit = _unitFactory.CreateUnit(id, new HexCoord(q, r), unitType, alliance);

                        int currentHealth = reader.IsDBNull(reader.GetOrdinal("current_health")) ? 100 : reader.GetInt32("current_health");
                        unit.Health = currentHealth;

                        map.AddUnit(unit);
                    }
                }
            }
        }

        public int SaveNewMap(HexMap map)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();

                try
                {
                    string insertMap = @"
                        INSERT INTO Maps (map_name, width, height, description, created_date, is_active) 
                        VALUES (@name, @width, @height, @description, NOW(), TRUE); 
                        SELECT LAST_INSERT_ID();";

                    int mapId;
                    using (var command = new MySqlCommand(insertMap, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@name", map.MapName ?? $"Level {map.Id}");
                        command.Parameters.AddWithValue("@width", map.Width);
                        command.Parameters.AddWithValue("@height", map.Height);
                        command.Parameters.AddWithValue("@description", map.Description ?? "");
                        mapId = Convert.ToInt32(command.ExecuteScalar());
                    }

                    for (int q = 0; q < map.Width; q++)
                    {
                        for (int r = 0; r < map.Height; r++)
                        {
                            if (map.Cells?[q, r] != null)
                            {
                                string insertCell = @"
                                    INSERT INTO MapCells (map_id, pos_q, pos_r, cell_type_id)
                                    VALUES (@mapId, @q, @r, (SELECT id FROM CellTypes WHERE type_name = @type))";

                                using (var command = new MySqlCommand(insertCell, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@mapId", mapId);
                                    command.Parameters.AddWithValue("@q", q);
                                    command.Parameters.AddWithValue("@r", r);
                                    command.Parameters.AddWithValue("@type", map.Cells[q, r].ToString());
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    if (map.Units != null)
                    {
                        foreach (var unit in map.Units)
                        {
                            if (unit != null)
                            {
                                int? unitId = GetUnitId(unit, connection, transaction);
                                if (unitId.HasValue)
                                {
                                    string insertUnit = @"
                                        INSERT INTO MapUnits (map_id, unit_id, pos_q, pos_r, current_health, status)
                                        VALUES (@mapId, @unitId, @q, @r, @health, 'Active')";

                                    using (var command = new MySqlCommand(insertUnit, connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@mapId", mapId);
                                        command.Parameters.AddWithValue("@unitId", unitId.Value);
                                        command.Parameters.AddWithValue("@q", unit.Position.Q);
                                        command.Parameters.AddWithValue("@r", unit.Position.R);
                                        command.Parameters.AddWithValue("@health", unit.Health);
                                        command.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }

                    transaction.Commit();
                    return mapId;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"Ошибка сохранения: {ex.Message}");
                    throw;
                }
            }
        }

        public void UpdateMap(HexMap map)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();

                try
                {
                    string updateMap = @"
                        UPDATE Maps 
                        SET map_name = @name, 
                            width = @width, 
                            height = @height,
                            description = @description
                        WHERE id = @mapId";

                    using (var command = new MySqlCommand(updateMap, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@mapId", map.Id);
                        command.Parameters.AddWithValue("@name", map.MapName ?? $"Level {map.Id}");
                        command.Parameters.AddWithValue("@width", map.Width);
                        command.Parameters.AddWithValue("@height", map.Height);
                        command.Parameters.AddWithValue("@description", map.Description ?? "");
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            throw new Exception($"Карта с ID {map.Id} не найдена");
                        }
                    }

                    string deleteCells = "DELETE FROM MapCells WHERE map_id = @mapId";
                    using (var command = new MySqlCommand(deleteCells, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@mapId", map.Id);
                        command.ExecuteNonQuery();
                    }

                    string deleteUnits = "DELETE FROM MapUnits WHERE map_id = @mapId";
                    using (var command = new MySqlCommand(deleteUnits, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@mapId", map.Id);
                        command.ExecuteNonQuery();
                    }

                    for (int q = 0; q < map.Width; q++)
                    {
                        for (int r = 0; r < map.Height; r++)
                        {
                            if (map.Cells?[q, r] != null)
                            {
                                string insertCell = @"
                                    INSERT INTO MapCells (map_id, pos_q, pos_r, cell_type_id)
                                    VALUES (@mapId, @q, @r, (SELECT id FROM CellTypes WHERE type_name = @type))";

                                using (var command = new MySqlCommand(insertCell, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@mapId", map.Id);
                                    command.Parameters.AddWithValue("@q", q);
                                    command.Parameters.AddWithValue("@r", r);
                                    command.Parameters.AddWithValue("@type", map.Cells[q, r].ToString());
                                    command.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    if (map.Units != null)
                    {
                        foreach (var unit in map.Units)
                        {
                            if (unit != null)
                            {
                                int? unitId = GetUnitId(unit, connection, transaction);
                                if (unitId.HasValue)
                                {
                                    string insertUnit = @"
                                        INSERT INTO MapUnits (map_id, unit_id, pos_q, pos_r, current_health, status)
                                        VALUES (@mapId, @unitId, @q, @r, @health, 'Active')";

                                    using (var command = new MySqlCommand(insertUnit, connection, transaction))
                                    {
                                        command.Parameters.AddWithValue("@mapId", map.Id);
                                        command.Parameters.AddWithValue("@unitId", unitId.Value);
                                        command.Parameters.AddWithValue("@q", unit.Position.Q);
                                        command.Parameters.AddWithValue("@r", unit.Position.R);
                                        command.Parameters.AddWithValue("@health", unit.Health);
                                        command.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"Ошибка обновления: {ex.Message}");
                    throw;
                }
            }
        }

        private int? GetUnitId(Unit unit, MySqlConnection connection, MySqlTransaction transaction)
        {
            string unitTypeStr = "";
            if (unit is Tank) unitTypeStr = "Tank";
            else if (unit is Infanity) unitTypeStr = "Infanity";
            else if (unit is Artillery) unitTypeStr = "Artillery";

            string query = "SELECT id FROM Units WHERE unit_type = @unitType AND alliance = @alliance LIMIT 1";

            using (var command = new MySqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@unitType", unitTypeStr);
                command.Parameters.AddWithValue("@alliance", unit.NameAlliance.ToString());

                var result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
            }

            return null;
        }

        public void DeleteMap(int mapId)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                var transaction = connection.BeginTransaction();

                try
                {
                    string deleteUnits = "DELETE FROM MapUnits WHERE map_id = @mapId";
                    using (var command = new MySqlCommand(deleteUnits, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@mapId", mapId);
                        command.ExecuteNonQuery();
                    }

                    string deleteCells = "DELETE FROM MapCells WHERE map_id = @mapId";
                    using (var command = new MySqlCommand(deleteCells, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@mapId", mapId);
                        command.ExecuteNonQuery();
                    }

                    string deleteMap = "DELETE FROM Maps WHERE id = @mapId";
                    using (var command = new MySqlCommand(deleteMap, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@mapId", mapId);
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            throw new Exception($"Карта с ID {mapId} не найдена");
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Diagnostics.Debug.WriteLine($"Ошибка удаления: {ex.Message}");
                    throw;
                }
            }
        }
    }
}