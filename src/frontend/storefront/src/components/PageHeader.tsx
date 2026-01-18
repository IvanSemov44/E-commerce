interface PageHeaderProps {
  title: string;
  subtitle?: string;
  showAccent?: boolean;
}

export default function PageHeader({ title, subtitle, showAccent = true }: PageHeaderProps) {
  return (
    <div>
      <h1>{title}</h1>
      {showAccent && <div></div>}
      {subtitle && <p>{subtitle}</p>}
    </div>
  );
}
