import csv, sqlite3, re
from pathlib import Path
root = Path(__file__).resolve().parents[1] / 'src/ETL.Opiniones'
output = Path(__file__).resolve().parent / 'warehouse_prueba.db'
if output.exists(): output.unlink()
con = sqlite3.connect(output)
con.executescript((root/'Sql/dimensiones.sql').read_text())
rows = []
with (root/'Data/encuestas.csv').open() as f:
    for r in csv.DictReader(f): rows.append(('CSV', r['Cliente'], r['Fecha'][:10]))
seed = (root/'Services/DemoDatabaseSeeder.cs').read_text()
for name, date in re.findall(r'\("([^"]+)", "[^"]+", "(\d{4}-\d{2}-\d{2})T[^\"]+"\)', seed):
    rows.append(('Base de datos', name, date))
# Registro sintetico para probar la ausencia de fecha de la API, no respuesta real.
rows.append(('API REST', 'Cliente de prueba API', None))
sql = (root/'Sql/cargar.sql').read_text()
for run in range(2):
    with con:
        for fuente, cliente, fecha in rows:
            for stmt in sql.split(';'):
                if stmt.strip(): con.execute(stmt, dict(fuente=fuente, cliente=cliente, fecha=fecha))
    counts = [con.execute('SELECT COUNT(*) FROM '+t).fetchone()[0] for t in ('DimFuente','DimCliente','DimFecha')]
    print(f'Corrida {run+1}: {len(rows)} entradas; dimensiones: {counts}')
    assert counts == [3,11,6]
for t in ('DimFuente','DimCliente','DimFecha'):
    print(t, con.execute('SELECT * FROM '+t).fetchall(), sep='\n')
print('Integridad:', con.execute('PRAGMA integrity_check').fetchone()[0])
con.close()
