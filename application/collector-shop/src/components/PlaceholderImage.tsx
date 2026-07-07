export function PlaceholderImage({ label, className = '' }: { label: string; className?: string }) {
  return (
    <div
      className={`flex aspect-square items-center justify-center border-[1.5px] border-ink ${className}`}
      style={{
        background:
          'repeating-linear-gradient(45deg, #e7dccb, #e7dccb 9px, #ddccb0 9px, #ddccb0 18px)',
      }}
    >
      <span className="rounded bg-surface px-2.5 py-1 font-ui text-[10px] font-semibold tracking-wide text-brown-3">
        {label}
      </span>
    </div>
  )
}
