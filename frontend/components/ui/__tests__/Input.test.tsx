import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { Input } from '../Input';

describe('Input', () => {
  it('should render an input element', () => {
    render(<Input />);
    expect(screen.getByRole('textbox')).toBeInTheDocument();
  });

  it('should render a label when label prop is provided', () => {
    render(<Input label='Email' />);
    expect(screen.getByText('Email')).toBeInTheDocument();
  });

  it('should not render a label when label prop is not provided', () => {
    render(<Input />);
    expect(screen.queryByRole('label')).not.toBeInTheDocument();
  });

  it('should display required asterisk when required prop is set', () => {
    render(<Input label='Email' required />);
    expect(screen.getByText('*')).toBeInTheDocument();
  });

  it('should not display required asterisk when required is not set', () => {
    render(<Input label='Email' />);
    expect(screen.queryByText('*')).not.toBeInTheDocument();
  });

  it('should render error message when error prop is provided', () => {
    render(<Input error='This field is required' />);
    expect(screen.getByText('This field is required')).toBeInTheDocument();
  });

  it('should apply error styles to input when error prop is provided', () => {
    render(<Input error='Error' />);
    const input = screen.getByRole('textbox');
    expect(input).toHaveClass('border-red-500');
  });

  it('should render helperText when provided and no error', () => {
    render(<Input helperText='Enter your email address' />);
    expect(screen.getByText('Enter your email address')).toBeInTheDocument();
  });

  it('should not render helperText when error is also provided', () => {
    render(<Input helperText='Helper text' error='Error message' />);
    expect(screen.queryByText('Helper text')).not.toBeInTheDocument();
    expect(screen.getByText('Error message')).toBeInTheDocument();
  });

  it('should be disabled when disabled prop is set', () => {
    render(<Input disabled />);
    expect(screen.getByRole('textbox')).toBeDisabled();
  });

  it('should accept and display a placeholder', () => {
    render(<Input placeholder='Enter text here' />);
    expect(screen.getByPlaceholderText('Enter text here')).toBeInTheDocument();
  });

  it('should pass type attribute to input', () => {
    render(<Input type='password' />);
    const input = document.querySelector('input[type="password"]');
    expect(input).toBeInTheDocument();
  });

  it('should merge custom className', () => {
    render(<Input className='custom-class' />);
    const input = screen.getByRole('textbox');
    expect(input).toHaveClass('custom-class');
  });

  it('should forward ref to the underlying input element', () => {
    const ref = React.createRef<HTMLInputElement>();
    render(<Input ref={ref} />);
    expect(ref.current).not.toBeNull();
    expect(ref.current?.tagName).toBe('INPUT');
  });

  it('should pass arbitrary HTML attributes to input', () => {
    render(<Input name='user-email' autoComplete='email' />);
    const input = screen.getByRole('textbox');
    expect(input).toHaveAttribute('name', 'user-email');
    expect(input).toHaveAttribute('autocomplete', 'email');
  });
});
