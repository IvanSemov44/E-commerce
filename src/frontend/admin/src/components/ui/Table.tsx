import React from 'react';
import styles from './Table.module.css';

interface Column<T> {
  header: string;
  accessor: (row: T) => React.ReactNode;
  width?: string;
}

interface TableProps<T> {
  columns: Column<T>[];
  data: T[];
  keyExtractor: (row: T) => string;
}

export default function Table<T>({ columns, data, keyExtractor }: TableProps<T>) {
  return (
    <div className={styles.tableContainer}>
      <table className={styles.table}>
        <thead>
          <tr>
            {columns.map((col, idx) => (
              <th key={idx} style={{ width: col.width }}>
                {col.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {data.length === 0 ? (
            <tr>
              <td colSpan={columns.length} className={styles.emptyState}>
                No data available
              </td>
            </tr>
          ) : (
            data.map((row) => (
              <tr key={keyExtractor(row)}>
                {columns.map((col, idx) => (
                  <td key={idx}>{col.accessor(row)}</td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}
