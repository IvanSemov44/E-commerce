import styles from './QuantityControl.module.css';

interface QuantityControlProps {
  value: number;
  max: number;
  min?: number;
  onChange: (value: number) => void;
  /** Renders an editable number input instead of a static display (for "add to cart" pickers) */
  editable?: boolean;
  disabled?: boolean;
}

export function QuantityControl({
  value,
  max,
  min = 1,
  onChange,
  editable = false,
  disabled = false,
}: QuantityControlProps) {
  return (
    <div className={styles.container}>
      <button
        onClick={() => onChange(Math.max(min, value - 1))}
        disabled={disabled || value <= min}
        aria-label="Decrease quantity"
        className={styles.button}
      >
        −
      </button>

      {editable ? (
        <input
          type="number"
          value={value}
          onChange={(e) => {
            const val = parseInt(e.target.value) || min;
            onChange(Math.min(max, Math.max(min, val)));
          }}
          min={min}
          max={max}
          disabled={disabled}
          className={styles.input}
        />
      ) : (
        <span className={styles.value}>{value}</span>
      )}

      <button
        onClick={() => onChange(Math.min(max, value + 1))}
        disabled={disabled || value >= max}
        aria-label="Increase quantity"
        className={styles.button}
      >
        +
      </button>

      {value >= max && <span className={styles.maxWarning}>Max stock reached</span>}
    </div>
  );
}
