export function CompanyLogo({ initials, color, size = 36 }: { initials: string; color: string; size?: number }) {
  return (
    <div
      className="flex items-center justify-center rounded-xl font-semibold text-white shrink-0"
      style={{ width: size, height: size, backgroundColor: color, fontSize: size * 0.36 }}
    >
      {initials}
    </div>
  );
}
