import Link from 'next/link';
import { Navbar } from '@/components/Navbar';

export default function TeamAppealRoute() {
  return (
    <>
      <Navbar />
      <main className="min-h-screen bg-surface-soft p-6 flex items-center justify-center">
        <div className="card max-w-xl text-center">
          <h1 className="t-heading-md m-0">Phúc khảo của đội</h1>
          <p className="t-body-sm text-mute">Hãy mở sự kiện đội đang tham gia và chọn tab Phúc khảo để gửi hoặc theo dõi đơn.</p>
          <Link href="/" className="btn btn-primary self-center">Về danh sách sự kiện</Link>
        </div>
      </main>
    </>
  );
}
