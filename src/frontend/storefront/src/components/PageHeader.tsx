interface PageHeaderProps {
  title: string;
  subtitle?: string;
  showAccent?: boolean;
}

export default function PageHeader({ title, subtitle, showAccent = true }: PageHeaderProps) {
  return (
    <div className="mb-8">
      <h1 className="heading-page mb-2">{title}</h1>
      {showAccent && <div className="underline-accent mb-4"></div>}
      {subtitle && <p className="text-body text-lg">{subtitle}</p>}
    </div>
  );
}
