import Link from 'next/link';
import { Navbar } from '@/components/Navbar';

export default function AppealsRoute() {
  return (
    <>
      <Navbar />
      <main className="min-h-screen bg-surface-soft p-6 flex items-center justify-center">
        <div className="card max-w-xl text-center">
          <h1 className="t-heading-md m-0">Phúc khảo theo sự kiện</h1>
          <p className="t-body-sm text-mute">Hãy chọn một sự kiện, sau đó mở tab Phúc khảo để xem dữ liệu phù hợp với vai trò của bạn.</p>
          <Link href="/" className="btn btn-primary self-center">Về danh sách sự kiện</Link>
        </div>
      </main>
    </>
  );
}
